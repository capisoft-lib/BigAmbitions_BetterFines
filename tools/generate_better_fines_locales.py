#!/usr/bin/env python3
"""Validate BetterFines locale JSON files share the same keys as en.json."""

from __future__ import annotations

import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "Locales"


def main() -> None:
    en_path = OUT / "en.json"
    en = json.loads(en_path.read_text(encoding="utf-8"))
    en_keys = set(en.keys())

    for path in sorted(OUT.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8"))
        keys = set(data.keys())
        if keys != en_keys:
            missing = en_keys - keys
            extra = keys - en_keys
            raise SystemExit(f"{path.name}: key mismatch missing={missing} extra={extra}")
        print(f"ok {path.name} ({len(keys)} keys)")

    print(f"done: {len(list(OUT.glob('*.json')))} locales validated")


if __name__ == "__main__":
    main()
