# Better Fines

**Better Fines** adds realistic traffic enforcement to Big Ambitions: speeding tickets, red-light cameras (with a camera flash), wrong-way detection, and a recidivism / license suspension system.

| | |
|---|---|
| **Game** | Big Ambitions EA **0.11 Experimental** |
| **Languages** | All **22** Big Ambitions interface languages |
| **Requires** | [`LIB_BaPlayerLocation`](https://github.com/capisoft-lib/BigAmbitions_LIB_BaPlayerLocation) |
| **Recommended** | [`Speedometer`](https://github.com/capisoft-lib/BigAmbitions_Speedometer) — optional HUD to see your speed while fines are enforced |
| **Author** | [capisoft-lib](https://github.com/capisoft-lib) — community mod, not affiliated with Hovgaard Games |

## Features

- **Speeding fines** — road speed limits with hold timer and on-screen warning
- **Red-light cameras** — visual traffic-light detection, camera flash, government SMS ticket
- **Wrong-way driving** — optional (off by default)
- **Recidivism** — escalating fines and license suspension
- **Fines status panel** — active tickets summary in the HUD

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

## Configuration

Optional `better_fines_config.json` lives next to the mod content (`ModContext.ModRootPath`). Copy `better_fines_config.json.example` to `ModsLocal/BetterFines/better_fines_config.json` for local tuning.

## Licence

[LICENSE](LICENSE)
