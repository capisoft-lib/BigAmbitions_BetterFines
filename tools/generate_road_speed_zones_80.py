#!/usr/bin/env python3
"""Bake 80 km/h road rectangles from Voogle enhanced routes + BetterFines speed limit dump."""

from __future__ import annotations

import argparse
import csv
import math
from pathlib import Path

DEFAULT_LIMIT_KMH = 50.0
HIGHWAY_LIMIT_KMH = 80.0
HALF_WIDTH_M = 10.0
SEGMENT_PAD_M = 1.0


def repo_root() -> Path:
    return Path(__file__).resolve().parents[5]


def load_speed_limits(path: Path) -> dict[int, float]:
    limits: dict[int, float] = {}
    with path.open(newline="", encoding="utf-8-sig") as handle:
        for row in csv.DictReader(handle):
            limits[int(row["list_index"])] = float(row["effective_limit_kmh"])
    return limits


def edge_limit_kmh(
    row: dict[str, str],
    limits: dict[int, float],
) -> float:
    from_idx = int(row["fromIndex"])
    to_idx = int(row["toIndex"])
    from_limit = limits.get(from_idx, DEFAULT_LIMIT_KMH)
    to_limit = limits.get(to_idx, DEFAULT_LIMIT_KMH)
    source = row.get("source", "")
    if source == "gley":
        return from_limit
    return min(from_limit, to_limit)


def oriented_zone(
    zone_id: int,
    row: dict[str, str],
    limit_kmh: float,
) -> dict[str, str] | None:
    fx = float(row["fromX"])
    fy = float(row["fromY"])
    fz = float(row["fromZ"])
    tx = float(row["toX"])
    ty = float(row["toY"])
    tz = float(row["toZ"])

    dx = tx - fx
    dz = tz - fz
    length = math.hypot(dx, dz)
    if length < 0.05:
        return None

    forward_x = dx / length
    forward_z = dz / length
    lateral_x = -forward_z
    lateral_z = forward_x
    center_x = (fx + tx) * 0.5
    center_y = (fy + ty) * 0.5
    center_z = (fz + tz) * 0.5
    half_length = length * 0.5 + SEGMENT_PAD_M
    half_width = HALF_WIDTH_M

    corners = (
        (center_x + forward_x * half_length + lateral_x * half_width, center_z + forward_z * half_length + lateral_z * half_width),
        (center_x + forward_x * half_length - lateral_x * half_width, center_z + forward_z * half_length - lateral_z * half_width),
        (center_x - forward_x * half_length + lateral_x * half_width, center_z - forward_z * half_length + lateral_z * half_width),
        (center_x - forward_x * half_length - lateral_x * half_width, center_z - forward_z * half_length - lateral_z * half_width),
    )
    xs = [c[0] for c in corners]
    zs = [c[1] for c in corners]

    return {
        "zone_id": str(zone_id),
        "edge_id": row.get("edgeId", ""),
        "edge_type": row.get("edgeType", ""),
        "source": row.get("source", ""),
        "from_index": row["fromIndex"],
        "to_index": row["toIndex"],
        "limit_kmh": f"{limit_kmh:.0f}",
        "center_x": f"{center_x:.6f}",
        "center_y": f"{center_y:.6f}",
        "center_z": f"{center_z:.6f}",
        "forward_x": f"{forward_x:.6f}",
        "forward_z": f"{forward_z:.6f}",
        "half_length_m": f"{half_length:.3f}",
        "half_width_m": f"{half_width:.3f}",
        "bounds_min_x": f"{min(xs):.6f}",
        "bounds_min_z": f"{min(zs):.6f}",
        "bounds_max_x": f"{max(xs):.6f}",
        "bounds_max_z": f"{max(zs):.6f}",
    }


def zone_corners(row: dict[str, str]) -> list[tuple[float, float]]:
    cx = float(row["center_x"])
    cz = float(row["center_z"])
    fx = float(row["forward_x"])
    fz = float(row["forward_z"])
    half_length = float(row["half_length_m"])
    half_width = float(row["half_width_m"])
    lx, lz = -fz, fx
    return [
        (cx + fx * half_length + lx * half_width, cz + fz * half_length + lz * half_width),
        (cx + fx * half_length - lx * half_width, cz + fz * half_length - lz * half_width),
        (cx - fx * half_length + lx * half_width, cz - fz * half_length + lz * half_width),
        (cx - fx * half_length - lx * half_width, cz - fz * half_length - lz * half_width),
    ]


def dominant_forward(zones: list[dict[str, str]]) -> tuple[float, float]:
    fx = 0.0
    fz = 0.0
    for row in zones:
        forward_x = float(row["forward_x"])
        forward_z = float(row["forward_z"])
        if fx == 0.0 and fz == 0.0:
            fx, fz = forward_x, forward_z
            continue
        if fx * forward_x + fz * forward_z < 0.0:
            forward_x, forward_z = -forward_x, -forward_z
        fx += forward_x
        fz += forward_z
    length = math.hypot(fx, fz)
    if length < 1e-6:
        return 1.0, 0.0
    return fx / length, fz / length


def merge_zones_single(zones: list[dict[str, str]]) -> list[dict[str, str]]:
    """Collapse aligned highway segments into one oriented corridor rectangle."""
    if len(zones) <= 1:
        return zones

    forward_x, forward_z = dominant_forward(zones)
    lateral_x, lateral_z = -forward_z, forward_x
    alongs: list[float] = []
    laterals: list[float] = []
    center_y = 0.0

    for row in zones:
        center_y += float(row["center_y"])
        for x, z in zone_corners(row):
            alongs.append(x * forward_x + z * forward_z)
            laterals.append(x * lateral_x + z * lateral_z)

    along_min = min(alongs)
    along_max = max(alongs)
    lateral_min = min(laterals)
    lateral_max = max(laterals)
    along_center = (along_min + along_max) * 0.5
    lateral_center = (lateral_min + lateral_max) * 0.5
    center_x = along_center * forward_x + lateral_center * lateral_x
    center_z = along_center * forward_z + lateral_center * lateral_z
    half_length = (along_max - along_min) * 0.5
    half_width = (lateral_max - lateral_min) * 0.5
    center_y /= len(zones)

    bounds_corners = zone_corners(
        {
            "center_x": f"{center_x}",
            "center_z": f"{center_z}",
            "forward_x": f"{forward_x}",
            "forward_z": f"{forward_z}",
            "half_length_m": f"{half_length}",
            "half_width_m": f"{half_width}",
        }
    )
    xs = [c[0] for c in bounds_corners]
    zs = [c[1] for c in bounds_corners]

    return [
        {
            "zone_id": "0",
            "edge_id": "merged_corridor",
            "edge_type": "merged",
            "source": "merged",
            "from_index": "",
            "to_index": "",
            "limit_kmh": f"{HIGHWAY_LIMIT_KMH:.0f}",
            "center_x": f"{center_x:.6f}",
            "center_y": f"{center_y:.6f}",
            "center_z": f"{center_z:.6f}",
            "forward_x": f"{forward_x:.6f}",
            "forward_z": f"{forward_z:.6f}",
            "half_length_m": f"{half_length:.3f}",
            "half_width_m": f"{half_width:.3f}",
            "bounds_min_x": f"{min(xs):.6f}",
            "bounds_min_z": f"{min(zs):.6f}",
            "bounds_max_x": f"{max(xs):.6f}",
            "bounds_max_z": f"{max(zs):.6f}",
        }
    ]


def build_zones(routes_path: Path, limits_path: Path) -> list[dict[str, str]]:
    limits = load_speed_limits(limits_path)
    zones: list[dict[str, str]] = []
    zone_id = 0

    with routes_path.open(newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            limit = edge_limit_kmh(row, limits)
            if limit < HIGHWAY_LIMIT_KMH - 0.5:
                continue

            zone = oriented_zone(zone_id, row, HIGHWAY_LIMIT_KMH)
            if zone is None:
                continue

            zones.append(zone)
            zone_id += 1

    return zones


def write_csv(path: Path, zones: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = [
        "zone_id",
        "edge_id",
        "edge_type",
        "source",
        "from_index",
        "to_index",
        "limit_kmh",
        "center_x",
        "center_y",
        "center_z",
        "forward_x",
        "forward_z",
        "half_length_m",
        "half_width_m",
        "bounds_min_x",
        "bounds_min_z",
        "bounds_max_x",
        "bounds_max_z",
    ]
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(zones)


def main() -> None:
    root = repo_root()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--routes",
        type=Path,
        default=root / "bigambitions/Assets/Mods/VoogleRoute/Data/big_ambitions_enhanced_routes.csv",
    )
    parser.add_argument(
        "--limits",
        type=Path,
        default=Path.home()
        / "AppData/LocalLow/Hovgaard Games/Big Ambitions/ModsLocal/BetterFines/Data/road_speed_limits.csv",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=root / "bigambitions/Assets/Mods/BetterFines/Data/road_speed_zones_80.csv",
    )
    parser.add_argument(
        "--merge",
        choices=("none", "single"),
        default="single",
        help="none=one rectangle per road segment; single=one corridor rectangle (default)",
    )
    parser.add_argument(
        "--input",
        type=Path,
        help="Merge an existing zones CSV instead of baking from routes/limits",
    )
    args = parser.parse_args()

    if args.input is not None:
        if not args.input.is_file():
            raise SystemExit(f"Missing input CSV: {args.input}")
        zones = list(csv.DictReader(args.input.open(newline="", encoding="utf-8-sig")))
    else:
        if not args.routes.is_file():
            raise SystemExit(f"Missing routes CSV: {args.routes}")
        if not args.limits.is_file():
            raise SystemExit(f"Missing speed limits CSV: {args.limits}")
        zones = build_zones(args.routes, args.limits)

    segment_count = len(zones)
    if args.merge == "single":
        zones = merge_zones_single(zones)

    write_csv(args.output, zones)
    if args.merge == "single" and segment_count != len(zones):
        print(f"Merged {segment_count} segments into {len(zones)} corridor zone(s)")
    print(f"Wrote {len(zones)} zones -> {args.output}")


if __name__ == "__main__":
    main()
