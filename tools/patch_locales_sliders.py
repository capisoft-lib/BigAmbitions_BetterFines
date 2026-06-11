#!/usr/bin/env python3
import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "Locales"
NEW = {
    "en": {
        "betterfines_options_fine_margin_percent": "Supplier margin (%)",
        "betterfines_options_fixed_fine_amount": "Fixed fine amount ($)",
        "betterfines_options_value_dollars": "${value}",
        "betterfines_options_value_percent": "{value}%",
    },
    "fr": {
        "betterfines_options_fine_margin_percent": "Marge fournalière (%)",
        "betterfines_options_fixed_fine_amount": "Montant fixe ($)",
        "betterfines_options_value_dollars": "{value} $",
        "betterfines_options_value_percent": "{value} %",
    },
}
INSERT = [
    "betterfines_options_fine_margin_percent",
    "betterfines_options_fixed_fine_amount",
    "betterfines_options_value_dollars",
    "betterfines_options_value_percent",
]

for path in sorted(OUT.glob("*.json")):
    data = json.loads(path.read_text(encoding="utf-8"))
    extra = NEW.get(path.stem, NEW["en"])
    data.update(extra)
    keys = [k for k in data if k not in INSERT]
    anchor = "betterfines_options_fine_mode_margin_percent"
    idx = keys.index(anchor) + 1 if anchor in keys else len(keys)
    for offset, key in enumerate(INSERT):
        keys.insert(idx + offset, key)
    ordered = {k: data[k] for k in keys}
    path.write_text(json.dumps(ordered, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("updated", path.name)
