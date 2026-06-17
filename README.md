# Better Fines

Looking for more driving realism?

**Better Fines** adds realistic traffic enforcement to Big Ambitions: speeding tickets, red-light cameras, wrong-way detection, pedestrian collision tickets (with [**Better Pedestrians**](https://github.com/capisoft-lib/BigAmbitions_BetterPedestrian)), government SMS notices, repeat-offense surcharges, and driver's license suspension.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Languages** | All **22** Big Ambitions interface languages |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) ([`LIB_BaUnifiedUI`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaUnifiedUI) is bundled in `Dependencies/`) |
| **Recommended** | [`Speedometer`](https://github.com/capisoft-lib/BigAmbitions_Speedometer) — optional HUD to see your speed while fines are enforced; [`Better Pedestrians`](https://github.com/capisoft-lib/BigAmbitions_BetterPedestrian) — **pedestrian hit fines** (detection & tuning; tickets issued via Better Fines) |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |
| **Source & updates** | [github.com/capisoft-lib/BigAmbitions_BetterFines](https://github.com/capisoft-lib/BigAmbitions_BetterFines) |

## Speed limits

- **50 km/h** — default city limit
- **80 km/h** — bridge / highway zones

Speeding is enforced only after you exceed the active limit by **10%** for a short hold period, with an on-screen warning.

## Features

| Feature | Default |
|---|---|
| Visual flash (camera flash on red-light tickets) | **on** |
| Speeding fines | **on** |
| Red-light fines | **on** |
| Orange-light fines | **off** |
| Wrong-way fines | **on** |
| Pedestrian hit fines | **off** without Better Pedestrians — **on** in Better Pedestrians options when both mods are enabled |
| Driver's license suspension | **on** |
| Repeat-offense surcharge | **on** |
| Government messages (SMS tickets & notices) | **on** |

Additional behaviour:

- **Repeat-offense surcharge** — escalating fines when you rack up tickets within a rolling window (+50% at 3 active fines, +100% at 5).
- **Driver's license suspension** — after 10 active fines, your license is suspended until outstanding tickets expire.
- **Fines status panel** — active tickets summary in the HUD.
- **Pedestrian hit fines** — Better Fines exposes the `Pedestrian` violation type, SMS template, and `BetterFinesFineApi`. Install and enable [**Better Pedestrians**](https://github.com/capisoft-lib/BigAmbitions_BetterPedestrian) to detect vehicle–pedestrian hits and charge tickets (toggle **Pedestrian hit fines** in its mod options).

## Options

**In-game (ESC → Options → Mods):** fine amounts, visual flash, each fine type, orange-light fines, and license suspension. The game stores these in PlayerPrefs automatically.

**Advanced tuning (optional `better_fines_config.json`):** detection thresholds, recidivism tiers, license-revoke count, logging, and debug flags. **No file is required** — defaults apply if absent. To tune advanced settings, copy `better_fines_config.json.example` from the mod source to `ModsLocal/BetterFines/better_fines_config.json`. Changes reload while playing (no restart needed).

Active fine records are stored in each save file via `save.modData` (not in the mod install folder). Legacy `active_fines_*.json` files in the mod folder are ignored and removed on load.

On first run after an upgrade, any old in-game keys still present in `better_fines_config.json` are migrated once into mod options (PlayerPrefs) and removed from the file.

### In-game mod options (PlayerPrefs)

| Setting | Default |
|---|---|
| Fine amount mode | fixed ($200) |
| Visual flash | **on** |
| Speeding fines | **on** |
| Red-light fines | **on** |
| Orange-light fines | **off** |
| Wrong-way fines | **on** |
| License suspension | **on** |

### Advanced JSON keys

| Key | Default | Description |
|---|---|---|
| `wrong_way_min_speed_kmh` | `8` | Minimum speed for wrong-way detection |
| `red_light_min_delay_sec` | `5` | Cooldown between red-light fines |
| `red_light_min_speed_kmh` | `3` | Minimum speed for red-light detection |
| `road_lookup_max_m` | `40` | Road segment search radius |
| `red_light_lookup_max_m` | `35` | Traffic-light search radius |
| `recidivism_enabled` | `true` | Repeat-offense surcharge |
| `fine_lifetime_days` | `5` | Days until a fine expires |
| `recidivism_tier1_count` | `3` | Active fines for +50% surcharge |
| `recidivism_tier1_percent` | `50` | Tier-1 surcharge percent |
| `recidivism_tier2_count` | `5` | Active fines for +100% surcharge |
| `recidivism_tier2_percent` | `100` | Tier-2 surcharge percent |
| `license_revoke_count` | `10` | Active fines before license suspension |
| `log_enabled` | `false` | Write mod logs to `Logs/` |
| `debug_red_light` | `false` | Red-light debug overlay |
| `debug_traffic_zones` | `false` | Traffic-zone debug overlay |
| `dump_road_speed_limits` | `false` | Dev CSV dump |
| `dump_traffic_approach_zones` | `false` | Dev CSV dump |
| `dump_traffic_light_visuals` | `false` | Dev CSV dump |

## Repository layout

This repository **is** the mod (flat layout — copy the repo root into `Assets/Mods/BetterFines/`).

```text
Scripts/ Locales/ tools/              Unity mod sources
ModManifest.asset  BetterFines.asmdef
better_fines_config.json.example      optional advanced-tuning template (repo only; not installed)
```

## Development

Requires [Big Ambitions Modding SDK](https://github.com/HovgaardGames/BigAmbitionsModding) (Unity **2022.3.62f2**) and [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation). [`Speedometer`](https://github.com/capisoft-lib/BigAmbitions_Speedometer) is optional but recommended for players. For **pedestrian hit fines**, also install [`Better Pedestrians`](https://github.com/capisoft-lib/BigAmbitions_BetterPedestrian) alongside Better Fines.

```bash
git clone https://github.com/capisoft-lib/BigAmbitions_BetterFines.git
```

1. Copy this repo into your SDK at `Assets/Mods/BetterFines/` (and install `LIB_BaPlayerLocation`).
2. **Mod Builder → Build + Install** for `LIB_BaPlayerLocation`, then `BetterFines`. After rebuilding **LIB_BaUnifiedUI**, run `.\tools\sync-dependencies.ps1` (or menu **Big Ambitions → Mods → Better Fines → Sync bundled dependencies**).

Or from a [BigAmbitions_DevEnv](https://github.com/capisoft-lib/BigAmbitions_DevEnv) workspace:

```powershell
bigambitions\scripts\compile-install-better-fines.ps1
```

Output: `%LocalLow%\Hovgaard Games\Big Ambitions\ModsLocal\BetterFines\`

## Licence

[LICENSE](LICENSE)
