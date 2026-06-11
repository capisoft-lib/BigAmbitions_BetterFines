# Better Fines

Looking for more driving realism?

**Better Fines** adds realistic traffic enforcement to Big Ambitions: speeding tickets, red-light cameras, wrong-way detection, government SMS notices, repeat-offense surcharges, and driver's license suspension.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Languages** | All **22** Big Ambitions interface languages |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) |
| **Recommended** | [`Speedometer`](https://github.com/capisoft-lib/BigAmbitions_Speedometer) — optional HUD to see your speed while fines are enforced |
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
| Driver's license suspension | **on** |
| Repeat-offense surcharge | **on** |
| Government messages (SMS tickets & notices) | **on** |

Additional behaviour:

- **Repeat-offense surcharge** — escalating fines when you rack up tickets within a rolling window (+50% at 3 active fines, +100% at 5).
- **Driver's license suspension** — after 10 active fines, your license is suspended until outstanding tickets expire.
- **Fines status panel** — active tickets summary in the HUD.

## Options

In-game mod options (and optional `better_fines_config.json`) let you:

- **Enable or disable** each fine type individually (visual flash, speeding, wrong-way, red light, orange light, license suspension).
- **Customise fine amounts** — fixed dollar amount (default **$200**) or percentage of vehicle value.

Copy `better_fines_config.json.example` to `ModsLocal/BetterFines/better_fines_config.json` for local tuning. The file lives next to the installed mod content (`ModContext.ModRootPath`).

Example keys:

| Key | Default | Description |
|---|---|---|
| `fine_amount_mode` | `"fixed"` | `"fixed"` or `"margin_percent"` |
| `fixed_fine_amount` | `200` | Dollar amount when mode is fixed |
| `fine_margin_percent` | `10` | % of vehicle value when mode is margin |
| `visual_flash_enabled` | `true` | Camera flash on red-light tickets |
| `speeding_enabled` | `true` | Speeding fines |
| `red_light_enabled` | `true` | Red-light fines |
| `red_light_orange_fine` | `false` | Orange-light fines |
| `wrong_way_enabled` | `true` | Wrong-way fines |
| `license_revoke_enabled` | `true` | License suspension |

## Repository layout

This repository **is** the mod (flat layout — copy the repo root into `Assets/Mods/BetterFines/`).

```text
Scripts/ Locales/ tools/              Unity mod sources
ModManifest.asset  BetterFines.asmdef
better_fines_config.json.example      runtime config template
```

## Development

Requires [Big Ambitions Modding SDK](https://github.com/HovgaardGames/BigAmbitionsModding) (Unity **2022.3.62f2**) and [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation). [`Speedometer`](https://github.com/capisoft-lib/BigAmbitions_Speedometer) is optional but recommended for players.

```bash
git clone https://github.com/capisoft-lib/BigAmbitions_BetterFines.git
```

1. Copy this repo into your SDK at `Assets/Mods/BetterFines/` (and install `LIB_BaPlayerLocation`).
2. **Mod Builder → Build + Install** for `LIB_BaPlayerLocation`, then `BetterFines`.

Or from a [BigAmbitions_DevEnv](https://github.com/capisoft-lib/BigAmbitions_DevEnv) workspace:

```powershell
bigambitions\scripts\compile-install-better-fines.ps1
```

Output: `%LocalLow%\Hovgaard Games\Big Ambitions\ModsLocal\BetterFines\`

## Licence

[LICENSE](LICENSE)
