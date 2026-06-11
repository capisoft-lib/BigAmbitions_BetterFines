#!/usr/bin/env python3
"""Use {amount}/{total} placeholders without hardcoded currency symbols in locale strings."""

from __future__ import annotations

import json
import re
from pathlib import Path

LOCALES = Path(__file__).resolve().parents[1] / "Locales"

REPLACEMENTS = [
    (re.compile(r"\$\{amount\}\.00"), "{amount}"),
    (re.compile(r"\{amount\} \$"), "{amount}"),
    (re.compile(r"\$\{total\}"), "{total}"),
    (re.compile(r"\{total\} \$"), "{total}"),
    (re.compile(r"\{type\} \$\{amount\}"), "{type} {amount}"),
    (re.compile(r"Total : \{total\} \$"), "Total : {total}"),
    (re.compile(r"\$\{value\}"), "{value}"),
    (re.compile(r"\{value\} \$"), "{value}"),
]


def main() -> None:
    for path in sorted(LOCALES.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        changed = False
        for key, value in list(data.items()):
            if not isinstance(value, str):
                continue
            updated = value
            for pattern, repl in REPLACEMENTS:
                updated = pattern.sub(repl, updated)
            if updated != value:
                data[key] = updated
                changed = True
        if changed:
            path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            print(f"updated {path.name}")
        else:
            print(f"ok {path.name}")


if __name__ == "__main__":
    main()
