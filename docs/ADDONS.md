# 🧩 Magnetar Client — Addons

Magnetar Client features an extensible **addon framework** that allows additional modules to hook into the client and game at runtime. Official addons are distributed alongside Magnetar Client releases and install seamlessly into the same mod directory.

> All addons require **Magnetar Client** to be installed first. See the [Installation Guide](INSTALLATION.md).

---

## 📦 Addon Distribution

Addons are distributed in two ways:

| Method | Description |
| :--- | :--- |
| **`-FULL.zip` releases** | Pre-bundled with the main client. Recommended for most users. Includes Assist Bot (Online) + Audio Addon pre-configured. |
| **`-Addons.zip` release** | Standalone archive with all variants of every addon (Online/Offline, MelonLoader/BepInEx). Use this to swap or add specific variants. |

---

## 1. 🤖 Assist Bot

An intelligent assistant for **The Gods Evolved** (Rogue Shooting) mode powered by a trained **ML.NET binary classification model**.

### What it does

Hooks into the buff selection screen and uses a machine learning model to score each candidate buff based on your current game state. Scores are displayed as color-coded percentage badges directly on the option buttons — **green** for high-confidence picks, **red** for low ones.

| Feature | Description |
| :--- | :--- |
| **Buff scoring** | Each option is assigned a probability score and displayed as a `[X%]` badge with a red→green gradient |
| **Auto-pick** | Automatically selects the highest-scoring buff after a configurable delay |
| **Live model sync** | Downloads the latest trained model from the server on startup *(Online variant)* |
| **Telemetry** | Records buff choices and uploads anonymized session data to continuously improve the model *(opt-in, Online variant)* |
| **Translation support** | Integrates with PvZ Fusion Translator to display buff names in your language |

### Settings

Accessible through Magnetar Client's module menu under **Assist Bot**.

**General**

| Setting | Default | Description |
| :--- | :--- | :--- |
| `Send feedback` | `true` | Upload anonymized session telemetry after each run to help improve the model |
| `Update on start` | `true` | Sync the latest prediction model from the server when the game loads |

**The Gods Evolved**

| Setting | Default | Description |
| :--- | :--- | :--- |
| `Suggest buffs` | `true` | Show color-coded percentage scores on each buff option |
| `Auto pick best buff` | `false` | Automatically select the top-scoring buff after the delay expires |
| `Auto pick delay` | `1.0s` | How long to wait before auto-picking (0–5 seconds) |

### Variants

Assist Bot comes in **4 variants** to suit different setups:

| Variant | Loader | Internet Required | Description |
| :--- | :--- | :--- | :--- |
| **Melonloader-Online** | MelonLoader | ✅ Yes | Syncs the latest prediction model on startup; uploads telemetry after runs |
| **Melonloader-Offline** | MelonLoader | ❌ No | Ships with a pre-bundled `buff_model.zip`; never connects to the internet |
| **BepInEx-Online** | BepInEx | ✅ Yes | Same as Melonloader-Online but for BepInEx |
| **BepInEx-Offline** | BepInEx | ❌ No | Same as Melonloader-Offline but for BepInEx |

> **Which variant should I use?**
> - Most players → **Online** (always gets the most accurate model automatically).
> - No internet or privacy-first → **Offline** (uses a snapshot model; still works well).

### How the Model Works

The model is a binary classifier trained on real player sessions. For each buff option it receives a **14-feature input vector**:

| Feature | Description |
| :--- | :--- |
| `Difficulty` | Active game difficulty |
| `CurrentPlantCount` | Plants currently owned |
| `MaxPlantCount` | Maximum plant slots available |
| `OwnedPlants` | List of plant IDs the player owns |
| `PlantTypeId` | Plant associated with the buff |
| `Quality` | Buff rarity / tier |
| `TimesModifierPicked` | How many times this buff was already taken this run |
| `CandidateTitle` | Buff name (used as a categorical identifier) |
| `PoolSize` | Number of options shown simultaneously |
| `CompetitorMaxQuality` | Highest quality among other options in the pool |
| `CompetitorAvgQuality` | Average quality of competing options |
| `Wave` | Continuous wave count across all boss regions |
| `ZombieLoadout` | Current zombie spawn archetype |
| `Label` | *(Training only)* Whether this buff was chosen |

The model outputs a `Probability` value (0–1). Each option's raw score is normalized against the pool total and displayed as a percentage. If no local model is present, a simple quality-based heuristic is used as a fallback.

### Installation

> Ensure [Magnetar Client](https://github.com/Tproplay/Magnetar-Client/releases) is already installed.

Assist Bot is included in all **FULL** release packages. For manual installation from `MagnetarClient-{version}-Addons.zip`:

**MelonLoader:**
1. Extract your chosen variant zip into your game root directory.
2. `Assist Bot.dll` → `[Game Folder]/Mods/Magnetar Data/Addons/`
3. `Microsoft.ML*.dll` and `System.Numerics.Tensors.dll` → `[Game Folder]/UserLibs/`
4. *(Offline only)* `buff_model.zip` → `[Game Folder]/Mods/Magnetar Data/Addons/ML/`

**BepInEx:**
1. Extract your chosen variant zip into your game root directory.
2. `Assist Bot.dll` → `[Game Folder]/BepInEx/plugins/Magnetar Data/Addons/`
3. `Microsoft.ML*.dll` and `System.Numerics.Tensors.dll` → `[Game Folder]/BepInEx/core/`
4. *(Offline only)* `buff_model.zip` → `[Game Folder]/BepInEx/plugins/Magnetar Data/Addons/ML/`

---

## 2. 🎵 Audio Addon

Dynamic in-game soundtrack and SFX replacement for Plants vs. Zombies Fusion.

### What it does

Replaces and customizes default game music and sound effects at runtime using dynamically downloaded audio bundles. No manual audio file management is needed — the addon handles streaming and caching automatically.

| Feature | Description |
| :--- | :--- |
| **Custom Soundtrack** | Replaces default game music with curated custom tracks |
| **SFX Replacement** | Swaps in themed sound effects |
| **Dynamic Download** | Audio assets are fetched and cached at runtime — no manual downloads needed |
| **MelonLoader & BepInEx** | Supported on both PC mod loaders |
| **Android** | Supported via BepInEx on the PVZRH Android Launcher |

### Installation

Audio Addon is included in all **FULL** release packages. For manual installation from `MagnetarClient-{version}-Addons.zip`, navigate into the `Audio Addon/` folder:

**MelonLoader (`Audio Addon/Melonloader/`):**
1. Copy `Mods/AudioImportLib.dll` → `[Game Folder]/Mods/`
2. Copy `Addons/AudioAddon.dll` → `[Game Folder]/Mods/Magnetar Data/Addons/`

**BepInEx (`Audio Addon/BepInEx/`):**
1. Copy `plugins/SherkeyXD.BepInEx-Sounds-Loader.dll` → `[Game Folder]/BepInEx/plugins/`
2. Copy `Addons/AudioAddon.dll` → `[Game Folder]/BepInEx/plugins/Magnetar Data/Addons/`

> **Audio files not downloading?** See [FAQ — Manual Audio Download](FAQ.md#2-audio-addon-is-showing-an-error-or-fails-to-download-the-soundtrack-automatically-where-can-i-download-audio-files-manually).

---

## 3. 🌱 Simple Spawner

A standalone utility mod developed as a separate project at [**github.com/Tproplay/Simple-Spawner**](https://github.com/Tproplay/Simple-Spawner).

### What it does

| Feature | Description |
| :--- | :--- |
| **Plant & Zombie Spawning** | Spawn any plant or zombie directly onto the lawn at custom coordinates |
| **Wave Control** | Manipulate wave spawns and timing |
| **Fusion Testing** | Test fusion interactions and design custom challenges |
| **Custom Challenges** | Build and replay custom lawn scenarios |

### Installation

Simple Spawner is included in PC **FULL** release packages. For standalone installation:

- **MelonLoader:** Place `SimpleSpawner.Melonloader.dll` into `[Game Folder]/Mods/`
- **BepInEx:** Place `SimpleSpawner.Bepinex.dll` into `[Game Folder]/BepInEx/plugins/`

> Simple Spawner has its own repository and release cycle at [github.com/Tproplay/Simple-Spawner](https://github.com/Tproplay/Simple-Spawner).

---

## Directory Structure Reference

After installing a **FULL** package the addon files land here:

**MelonLoader:**
```text
[Game Directory]/
├── Mods/
│   ├── Magnetar Data/
│   │   └── Addons/
│   │       ├── Assist Bot.dll
│   │       └── AudioAddon.dll
│   ├── AudioImportLib.dll
│   └── SimpleSpawner.Melonloader.dll
└── UserLibs/
    └── Microsoft.ML*.dll   ← Assist Bot ML libraries
```

**BepInEx:**
```text
[Game Directory]/
├── BepInEx/
│   ├── core/
│   │   └── Microsoft.ML*.dll   ← Assist Bot ML libraries
│   └── plugins/
│       ├── Magnetar Data/
│       │   └── Addons/
│       │       ├── Assist Bot.dll
│       │       └── AudioAddon.dll
│       ├── SherkeyXD.BepInEx-Sounds-Loader.dll
│       └── SimpleSpawner.Bepinex.dll
```

---

## ❓ More Help

- [Installation Guide](INSTALLATION.md) — Full step-by-step setup for all platforms.
- [FAQ](FAQ.md) — Common questions, game version compatibility, and troubleshooting.
- [Assist Bot Source](../Addons/Assist%20Bot/README.md) — Technical details and build instructions.
- [Audio Addon Source](../Addons/Audio%20Addon/README.md) — Source and build instructions.
