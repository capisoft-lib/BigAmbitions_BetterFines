# Changelog

## 0.11.4

- **Fines HUD lag fix** — status panel no longer destroys/rebuilds every tick after the first ticket
- **LIB_BaUnifiedUI 0.1.5 bundled** — `BaUiPanelHost` lifecycle fix for lazy HUD panels; `NonInteractive()` overlay builder
- **Status panel polish** — docked chrome restore, cached position, raycast targets off on read-only HUD

## 0.11.3

- **LIB_BaUnifiedUI bundled** — fines status panel and HUD chrome (no separate Workshop mod)
- **Mod options** — ESC → Options → Mods via PlayerPrefs; repair stale defaults (all on except orange light)
- **Per-save fine storage** — active tickets in `save.modData`; legacy `active_fines_*.json` removed on load
- **Better Pedestrians bridge** — pedestrian fines gated on companion mod
- **Optional advanced config** — JSON for debug/tuning only; not required for players

## 0.11.2

- Pedestrian violation type and `BetterFinesFineApi` for Better Pedestrians integration

## 0.11.1

- Enforcement polish, mod options UI, traffic data indexes

## 0.11.0

- Initial Better Fines release for Big Ambitions SDK 0.11
