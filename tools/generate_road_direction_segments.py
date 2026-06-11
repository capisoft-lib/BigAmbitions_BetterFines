#!/usr/bin/env python3
"""Bake oriented road direction segments from Voogle enhanced routes (Gley edges)."""

from __future__ import annotations

import argparse
import csv
import math
from pathlib import Path

HALF_WIDTH_M = 6.0
SEGMENT_PAD_M = 1.0


def repo_root() -> Path:
    return Path(__file__).resolve().parents[5]


def oriented_segment(segment_id: int, row: dict[str, str]) -> dict[str, str] | None:
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
        "segment_id": str(segment_id),
        "edge_id": row.get("edgeId", ""),
        "edge_type": row.get("edgeType", ""),
        "source": row.get("source", ""),
        "from_index": row["fromIndex"],
        "to_index": row["toIndex"],
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


def build_segments(routes_path: Path) -> list[dict[str, str]]:
    segments: list[dict[str, str]] = []
    segment_id = 0

    with routes_path.open(newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if row.get("source", "") != "gley":
                continue

            segment = oriented_segment(segment_id, row)
            if segment is None:
                continue

            segments.append(segment)
            segment_id += 1

    return segments


def write_csv(path: Path, segments: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = [
        "segment_id",
        "edge_id",
        "edge_type",
        "source",
        "from_index",
        "to_index",
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
        writer.writerows(segments)


def main() -> None:
    root = repo_root()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--routes",
        type=Path,
        default=root / "bigambitions/Assets/Mods/VoogleRoute/Data/big_ambitions_enhanced_routes.csv",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=root / "bigambitions/Assets/Mods/BetterFines/Data/road_direction_segments.csv",
    )
    args = parser.parse_args()

    if not args.routes.is_file():
        raise SystemExit(f"Missing routes CSV: {args.routes}")

    segments = build_segments(args.routes)
    write_csv(args.output, segments)
    print(f"Wrote {len(segments)} segments -> {args.output}")


if __name__ == "__main__":
    main()
