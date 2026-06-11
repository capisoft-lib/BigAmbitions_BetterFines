#!/usr/bin/env python3
"""Render an SVG map of 80 km/h speed zones (with faint road context)."""

from __future__ import annotations

import argparse
import csv
import math
from pathlib import Path

VB_W = 1800
VB_H = 1500
MAP_MARGIN = 40


def repo_root() -> Path:
    return Path(__file__).resolve().parents[5]


def load_zones(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8-sig") as handle:
        return list(csv.DictReader(handle))


def load_route_segments(path: Path) -> list[tuple[float, float, float, float]]:
    segments: list[tuple[float, float, float, float]] = []
    with path.open(newline="", encoding="utf-8-sig") as handle:
        for row in csv.DictReader(handle):
            if row.get("edgeType") != "base":
                continue
            segments.append(
                (
                    float(row["fromX"]),
                    float(row["fromZ"]),
                    float(row["toX"]),
                    float(row["toZ"]),
                )
            )
    return segments


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
        (cx - fx * half_length - lx * half_width, cz - fz * half_length - lz * half_width),
        (cx - fx * half_length + lx * half_width, cz - fz * half_length + lz * half_width),
    ]


def collect_bounds(
    zones: list[dict[str, str]],
    segments: list[tuple[float, float, float, float]],
) -> tuple[float, float, float, float]:
    xs: list[float] = []
    zs: list[float] = []

    for row in zones:
        for x, z in zone_corners(row):
            xs.append(x)
            zs.append(z)

    for fx, fz, tx, tz in segments:
        xs.extend((fx, tx))
        zs.extend((fz, tz))

    return min(xs), max(xs), min(zs), max(zs)


def build_transform(min_x: float, max_x: float, min_z: float, max_z: float):
    scale = min(
        (VB_W - MAP_MARGIN * 2) / (max_x - min_x),
        (VB_H - MAP_MARGIN * 2) / (max_z - min_z),
    )
    used_w = (max_x - min_x) * scale
    used_h = (max_z - min_z) * scale
    off_x = (VB_W - used_w) / 2
    off_y = (VB_H - used_h) / 2

    def to_svg(x: float, z: float) -> tuple[float, float]:
        return off_x + (x - min_x) * scale, off_y + (max_z - z) * scale

    return to_svg


def polygon_points(corners: list[tuple[float, float]], to_svg) -> str:
    return " ".join(f"{to_svg(x, z)[0]:.2f},{to_svg(x, z)[1]:.2f}" for x, z in corners)


def build_svg(
    zones: list[dict[str, str]],
    segments: list[tuple[float, float, float, float]],
) -> str:
    min_x, max_x, min_z, max_z = collect_bounds(zones, segments)
    to_svg = build_transform(min_x, max_x, min_z, max_z)

    lines = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{VB_W}" height="{VB_H}" viewBox="0 0 {VB_W} {VB_H}">',
        "  <desc>BetterFines 80 km/h speed zones (orange) over base Gley roads (gray).</desc>",
        "  <style>",
        "    .bg { fill: #0e151b; }",
        "    .road { fill: none; stroke: #4f6470; stroke-width: 1.1; stroke-linecap: round; opacity: 0.35; }",
        "    .zone80 { fill: #ff8c2a; fill-opacity: 0.42; stroke: #ffb347; stroke-width: 0.8; }",
        "    .title { fill: #e8f0f4; font: 28px Arial, sans-serif; font-weight: 700; }",
        "    .legend { fill: #c5d4dc; font: 16px Arial, sans-serif; }",
        "    .legend-box { stroke: #7a909c; stroke-width: 1; }",
        "  </style>",
        '  <rect class="bg" x="0" y="0" width="100%" height="100%" />',
        '  <text class="title" x="28" y="42">BetterFines — zones 80 km/h</text>',
        f'  <text class="legend" x="28" y="72">{len(zones)} segments | default elsewhere: 50 km/h</text>',
        '  <g id="roads">',
    ]

    for fx, fz, tx, tz in segments:
        x1, y1 = to_svg(fx, fz)
        x2, y2 = to_svg(tx, tz)
        lines.append(f'    <line class="road" x1="{x1:.2f}" y1="{y1:.2f}" x2="{x2:.2f}" y2="{y2:.2f}" />')

    lines.append("  </g>")
    lines.append('  <g id="zones-80">')

    for row in zones:
        corners = zone_corners(row)
        points = polygon_points(corners, to_svg)
        zone_id = row["zone_id"]
        lines.append(f'    <polygon class="zone80" data-zone-id="{zone_id}" points="{points}" />')

    lines.extend(
        [
            "  </g>",
            '  <g id="legend" transform="translate(28, 92)">',
            '    <rect class="legend-box" x="0" y="0" width="220" height="58" fill="rgba(255,255,255,0.05)" rx="6" />',
            '    <rect x="14" y="16" width="28" height="14" class="zone80" />',
            '    <text class="legend" x="52" y="28">80 km/h zone</text>',
            '    <line class="road" x1="14" y1="46" x2="42" y2="46" />',
            '    <text class="legend" x="52" y="50">base road (context)</text>',
            "  </g>",
            "</svg>",
        ]
    )
    return "\n".join(lines) + "\n"


def main() -> None:
    root = repo_root()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--zones",
        type=Path,
        default=root / "bigambitions/Assets/Mods/BetterFines/Data/road_speed_zones_80.csv",
    )
    parser.add_argument(
        "--routes",
        type=Path,
        default=root / "bigambitions/Assets/Mods/VoogleRoute/Data/big_ambitions_enhanced_routes.csv",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=root / "bigambitions/Assets/Mods/BetterFines/tools/road_speed_zones_80_map.svg",
    )
    args = parser.parse_args()

    if not args.zones.is_file():
        raise SystemExit(f"Missing zones CSV: {args.zones}")

    zones = load_zones(args.zones)
    segments = load_route_segments(args.routes) if args.routes.is_file() else []
    svg = build_svg(zones, segments)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(svg, encoding="utf-8")
    print(f"Wrote map ({len(zones)} zones) -> {args.output}")


if __name__ == "__main__":
    main()
