#!/usr/bin/env python3
"""Replace hardcoded SMS signatures with {department} and add department locale keys."""

from __future__ import annotations

import json
import re
from pathlib import Path

LOCALES = Path(__file__).resolve().parents[1] / "Locales"

EN_TRAFFIC = "The New York City Department of Transportation"
EN_DMV = "The New York State Department of Motor Vehicles"
FR_TRAFFIC = "Le Département des Transports de la Ville de New York"
FR_DMV = "Le Département des Véhicules Motorisés de l'État de New York"


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def save(path: Path, data: dict) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    for path in sorted(LOCALES.glob("*.json")):
        data = load(path)
        if path.name == "fr.json":
            data["betterfines_sms_department_traffic"] = FR_TRAFFIC
            data["betterfines_sms_department_motor_vehicles"] = FR_DMV
        else:
            data["betterfines_sms_department_traffic"] = EN_TRAFFIC
            data["betterfines_sms_department_motor_vehicles"] = EN_DMV

        for key, value in list(data.items()):
            if not key.startswith("betterfines:sms_government"):
                continue
            if "{department}" not in value:
                data[key] = re.sub(r"<br>[^<]+$", "<br>{department}", value)

        save(path, data)
        print(f"ok {path.name}")

    en_keys = set(load(LOCALES / "en.json").keys())
    for path in sorted(LOCALES.glob("*.json")):
        keys = set(load(path).keys())
        if keys != en_keys:
            raise SystemExit(f"{path.name}: key mismatch")
    print(f"validated {len(en_keys)} keys")


if __name__ == "__main__":
    main()
