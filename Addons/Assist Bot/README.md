# Assist Bot
> A Magnetar Client addon that uses machine learning to recommend and auto-pick the best buffs in Plants vs. Zombies Fusion — *The Gods Evolved* mode.

[![Version](https://img.shields.io/badge/version-4.0.3-blue)](#)
[![License](https://img.shields.io/badge/license-GPLv3-green)](LICENSE.txt)
[![Target](https://img.shields.io/badge/.NET-6.0-purple)](#)

---

## Overview

Assist Bot hooks into the buff selection screen during *The Gods Evolved* (Rogue Shooting) runs and uses a trained **`ML.NET` binary classification model** to score each candidate buff based on your current game state. Scores are displayed as color-coded percentages directly on the option buttons — green for high-confidence picks, red for low ones.

The addon also silently collects anonymized gameplay telemetry (opt-in) and uploads it to a server after each run. This data is used to continuously retrain and improve the prediction model, which is then pushed back to all clients.

---

## Features

| Feature | Description |
|---|---|
| **Buff scoring** | Each option is assigned a probability score from the ML model and displayed as a `[X%]` badge with a red→green gradient |
| **Auto-pick** | Automatically selects the highest-scoring buff after a configurable delay |
| **Live model sync** | Downloads the latest trained model from the server on startup (optional) |
| **Telemetry** | Records your buff choices in a tamper-proof local CSV and uploads it after each completed run (optional) |
| **Translation support** | Integrates with PvZ Fusion Translator to display buff names in your language |

---

## Settings

All settings are accessible through the Magnetar Client module menu under **Assist Bot**.

### General
| Setting | Default | Description |
|---|---|---|
| `Send feedback` | `true` | Upload anonymized session telemetry after each run to help improve the model |
| `Update on start` | `true` | Sync the latest prediction model from the server when the game loads |

### The Gods Evolved
| Setting | Default | Description |
|---|---|---|
| `Suggest buffs` | `true` | Show color-coded percentage scores on each buff option |
| `Auto pick best buff` | `false` | Automatically select the top-scoring buff after the delay expires |
| `Auto pick delay` | `1.0s` | How long to wait before auto-picking (0–5 seconds) |

---

## How It Works

### Prediction Model
The model is a binary classifier trained on real player sessions. For each buff option shown, it receives a **14-feature input vector**:

| Feature | Description |
|---|---|
| `Difficulty` | Active game difficulty |
| `CurrentPlantCount` | Plants currently owned |
| `MaxPlantCount` | Maximum plant slots available |
| `OwnedPlants` | List of plant IDs the player owns |
| `PlantTypeId` | Plant associated with the buff |
| `Quality` | Buff rarity/tier |
| `TimesModifierPicked` | How many times this buff was already taken this run |
| `CandidateTitle` | Buff name (Chinese, used as a categorical identifier) |
| `PoolSize` | Number of options shown simultaneously |
| `CompetitorMaxQuality` | Highest quality among other options in the pool |
| `CompetitorAvgQuality` | Average quality of competing options |
| `Wave` | Continuous wave count across all boss regions |
| `ZombieLoadout` | Current zombie spawn archetype |
| `Label` | *(Training only)* Whether this buff was chosen |

The model outputs a `Probability` value (0–1). Each option's raw score is normalized against the pool total and displayed as a percentage.

If no local model file is present, a simple quality-based heuristic is used as a fallback.

---

## Building from Source

### 1. Set up your secret key

The telemetry HMAC key is not included in this repository. Copy the example file and fill in the key you received:

```
Project/Src/Secrets.cs.example  →  Project/Src/Secrets.cs
```

Edit `Secrets.cs` and replace `YOUR_HMAC_SECRET_HERE` with the actual key. **Never commit this file.**

### 2. Build

Four configurations are available:

| Configuration | Loader | Obfuscated | Output |
|---|---|---|---|
| `MelonLoader` | MelonLoader | ✅ Yes | `bin\Melonloader\MelonLoader\` |
| `BepInEx` | BepInEx | ✅ Yes | `bin\BepInEx\BepInEx\` |
| `MELON LOCAL` | MelonLoader | ❌ No | `bin\Melonloader\MELON LOCAL\` |
| `BEPINEX LOCAL` | BepInEx | ❌ No | `bin\BepInEx\BEPINEX LOCAL\` |

```bash
# MelonLoader release (obfuscated)
dotnet build "Project/Assist Bot.csproj" -c MelonLoader

# BepInEx local dev build
dotnet build "Project/Assist Bot.csproj" -c "BEPINEX LOCAL"
```

The release configurations (`MelonLoader` / `BepInEx`) automatically run **Obfuscar** after the build to protect the assembly.

---

## Installation

> Ensure [Magnetar Client](https://github.com/Tproplay/Magnetar-Client/releases) is installed in your game first

1. [Download the addon](https://github.com/Tproplay/Magnetar-Client/releases) using the appropriate configuration for your mod loader.
2. Copy `Assist Bot.dll` to your Magnetar Client addons directory:
   - **MelonLoader**: `<GameRoot>/Mods/Magnetar Data/Addons/`
   - **BepInEx**: `<GameRoot>/BepInEx/plugins/Magnetar Data/Addons/`
3. Launch the game. The addon will initialize automatically and appear in the Magnetar module menu.
4. *(Optional)* Enable `Update on start` to pull the latest model on first run.

---

## Author

**Tproplay** — part of the [Magnetar Client](https://github.com/Tproplay) project.

---

## License

This project is licensed under the **GNU General Public License v3.0**. See [LICENSE.txt](LICENSE.txt) for the full terms.
