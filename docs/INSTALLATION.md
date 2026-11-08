# Magnetar Client & Addons — Installation Guide

This guide covers how to install **Magnetar Client**, **FULL release packages**, and standalone **Addons** for Plants vs. Zombies Fusion across **MelonLoader**, **BepInEx**, and **Android**.

---

## 📦 Release Packages Overview

When downloading releases from [Releases](https://github.com/Tproplay/Magnetar-Client/releases), you will find the following packages:

| Package | Description | Recommended For |
|---|---|---|
| **`MagnetarClient-{version}-Melonloader.zip`** | Base Magnetar Client for MelonLoader (PC). | Users who only want the core client. |
| **`MagnetarClient-{version}-Melonloader-FULL.zip`** | **All-in-One Pack**: Magnetar Client + Simple Spawner + Assist Bot (Online) + Audio Addon. | Most MelonLoader users. |
| **`MagnetarClient-{version}-Bepinex.zip`** | Base Magnetar Client for BepInEx (PC). | Users who only want the core client. |
| **`MagnetarClient-{version}-Bepinex-FULL.zip`** | **All-in-One Pack**: Magnetar Client + Simple Spawner + Assist Bot (Online) + Audio Addon. | Most BepInEx users. |
| **`MagnetarClient-{version}-Android.zip`** | Base Magnetar Client for Android (PVZRH Launcher). | Android users wanting only core client. |
| **`MagnetarClient-{version}-Android-FULL.zip`** | **All-in-One Pack**: Magnetar Client + Assist Bot (Online) + Audio Addon. | Android users wanting all addons. |
| **`MagnetarClient-{version}-Addons.zip`** | Contains standalone packages for **Assist Bot** (all 4 variants) and **Audio Addon**. | Adding or customizing specific addons. |

---

## 🍉 MelonLoader Installation (PC)

> **Prerequisite:** Ensure you have [MelonLoader](https://melonwiki.xyz/) installed on your game.

### Option A: Installing the FULL Pack (Recommended)
1. Download `MagnetarClient-{version}-Melonloader-FULL.zip`.
2. Extract the archive into your root game directory (`[Your Game Folder]\`):
   - `Mods` folder contents merge into `[Game Folder]\Mods` (includes Magnetar Client, Magnetar Data, Simple Spawner, and Assist Bot).
   - `UserLibs` folder contents merge into `[Game Folder]\UserLibs` (includes DiscordRPC, Newtonsoft.Json, and Machine Learning libraries).
3. Launch `PlantsVsZombiesRH.exe`.

### Option B: Installing the Base Client Only
1. Download `MagnetarClient-{version}-Melonloader.zip`.
2. Extract the archive into your root game directory:
   - `Mods` folder contents merge into `[Game Folder]\Mods`.
   - `UserLibs` folder contents merge into `[Game Folder]\UserLibs`.
3. Launch `PlantsVsZombiesRH.exe`.

> **Note:** If you are running the multi-language version of the game, remove `Blooms_QOL.dll` from `Mods\` if present to avoid conflicts.

#### MelonLoader Directory Structure
```text
[Game Directory]/
├── MelonLoader/
├── Mods/
│   ├── Magnetar Data/
│   │   ├── Addons/
│   │   │   ├── Assist Bot.dll   <-- (Included in FULL pack)
│   │   │   └── AudioAddon.dll   <-- (Included in FULL pack)
│   │   ├── Magnetar Translation/
│   │   ├── Resources/
│   │   └── Themes/
│   ├── AudioImportLib.dll       <-- (Included in FULL pack)
│   ├── Magnetar Client.dll
│   └── SimpleSpawner.Melonloader.dll <-- (Included in FULL pack)
├── UserLibs/
│   ├── DiscordRPC.dll
│   ├── Newtonsoft.Json.dll
│   └── Microsoft.ML*.dll        <-- (Included in FULL pack)
├── PlantsVsZombiesRH.exe
└── version.dll
```

---

## ⚡ BepInEx Installation (PC)

> **Prerequisite:** Ensure you have [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) (IL2CPP) installed on your game.

### Option A: Installing the FULL Pack (Recommended)
1. Download `MagnetarClient-{version}-Bepinex-FULL.zip`.
2. Extract the archive into your root game directory (`[Your Game Folder]\`):
   - `BepInEx\plugins` contents merge into `[Game Folder]\BepInEx\plugins`.
   - `BepInEx\core` contents merge into `[Game Folder]\BepInEx\core` (includes Newtonsoft.Json and Machine Learning libraries).
3. Launch `PlantsVsZombiesRH.exe`.

### Option B: Installing the Base Client Only
1. Download `MagnetarClient-{version}-Bepinex.zip`.
2. Extract the archive into your root game directory:
   - `BepInEx\plugins` contents merge into `[Game Folder]\BepInEx\plugins`.
   - `BepInEx\core` contents merge into `[Game Folder]\BepInEx\core`.
3. Launch `PlantsVsZombiesRH.exe`.

#### BepInEx Directory Structure
```text
[Game Directory]/
├── BepInEx/
│   ├── core/
│   │   ├── Newtonsoft.Json.dll
│   │   └── Microsoft.ML*.dll        <-- (Included in FULL pack)
│   └── plugins/
│       ├── Magnetar Data/
│       │   ├── Addons/
│       │   │   ├── Assist Bot.dll   <-- (Included in FULL pack)
│       │   │   └── AudioAddon.dll   <-- (Included in FULL pack)
│       │   ├── Magnetar Translation/
│       │   ├── Resources/
│       │   └── Themes/
│       ├── DiscordRPC.dll
│       ├── Magnetar Client.dll
│       ├── SherkeyXD.BepInEx-Sounds-Loader.dll <-- (Included in FULL pack)
│       └── SimpleSpawner.Bepinex.dll <-- (Included in FULL pack)
├── PlantsVsZombiesRH.exe
└── winhttp.dll
```

---

## 📱 Android Installation (PVZRH Launcher)

1. Download and install the [latest PVZRH Android Launcher](https://github.com/ModPVZRH/PVZRH.Android.Launcher/releases).
2. Open the launcher once to create default directories, then close it.
3. Download `MagnetarClient-{version}-Android-FULL.zip` (or `MagnetarClient-{version}-Android.zip`).
4. Re-open the PVZRH Launcher.
5. Tap the **Modpacks** tab (the 2nd icon at the bottom of the screen).
6. Tap the **Curled Page icon** in the top-right corner.
7. Select the downloaded `.zip` file to import it.

---

## 🧩 Addons Installation Guide

If you downloaded `MagnetarClient-{version}-Addons.zip`, extract it to access the individual addon packages:

### 1. 🤖 Assist Bot

Assist Bot has 4 package variants inside `Assist Bot/`:

| Variant | Type | Description |
|---|---|---|
| `MagnetarClient.AssistBot-{version}-Melonloader-Online.zip` | MelonLoader | Connects to server to sync latest prediction models and upload telemetry. |
| `MagnetarClient.AssistBot-{version}-Melonloader-Offline.zip` | MelonLoader | Includes offline pre-trained `buff_model.zip`. Never requires internet connection. |
| `MagnetarClient.AssistBot-{version}-BepInEx-Online.zip` | BepInEx | Connects to server to sync latest prediction models and upload telemetry. |
| `MagnetarClient.AssistBot-{version}-BepInEx-Offline.zip` | BepInEx | Includes offline pre-trained `buff_model.zip`. Never requires internet connection. |

#### Installation Steps:
* **For MelonLoader:**
  1. Extract the selected zip into your game directory.
  2. `Assist Bot.dll` goes into `Mods/Magnetar Data/Addons/Assist Bot.dll`.
  3. Machine learning libraries (`Microsoft.ML*.dll` and `System.Numerics.Tensors.dll`) go into `UserLibs/`.
  4. *(Offline variant only)* `buff_model.zip` is placed in `Mods/Magnetar Data/Addons/ML/buff_model.zip`.

* **For BepInEx:**
  1. Extract the selected zip into your game directory.
  2. `Assist Bot.dll` goes into `BepInEx/plugins/Magnetar Data/Addons/Assist Bot.dll`.
  3. Machine learning libraries (`Microsoft.ML*.dll` and `System.Numerics.Tensors.dll`) go into `BepInEx/core/`.
  4. *(Offline variant only)* `buff_model.zip` is placed in `BepInEx/plugins/Magnetar Data/Addons/ML/buff_model.zip`.

---

### 2. 🎵 Audio Addon

Audio Addon replaces and customizes game music and sound effects on the fly. Audio bundles and sound packs are downloaded dynamically by the mod at runtime.

Located inside `Audio Addon/` in `MagnetarClient-{version}-Addons.zip`:

* **For MelonLoader (`Audio Addon/Melonloader/`):**
  1. Copy `Mods/AudioImportLib.dll` into your game's `Mods/` folder.
  2. Copy `Addons/AudioAddon.dll` into `Mods/Magnetar Data/Addons/AudioAddon.dll`.

* **For BepInEx (`Audio Addon/BepInEx/`):**
  1. Copy `plugins/SherkeyXD.BepInEx-Sounds-Loader.dll` into your game's `BepInEx/plugins/` folder.
  2. Copy `Addons/AudioAddon.dll` into `BepInEx/plugins/Magnetar Data/Addons/AudioAddon.dll`.

---

### 3. 🌱 Simple Spawner

A standalone plant and zombie spawner, wave modifier, and custom testing companion developed at [**https://github.com/Tproplay/Simple-Spawner**](https://github.com/Tproplay/Simple-Spawner).

* **MelonLoader:** Place `SimpleSpawner.Melonloader.dll` into `[Game Folder]/Mods/`.
* **BepInEx:** Place `SimpleSpawner.Bepinex.dll` into `[Game Folder]/BepInEx/plugins/`.

*(Note: Simple Spawner is already included out-of-the-box in the PC `FULL` releases for convenience).*

---

## ❓ Frequently Asked Questions

For the complete list of questions, compatibility details, and troubleshooting, visit the **[FAQ Guide](FAQ.md)**.

* **Game compatibility:** All supported game versions are listed on the [GitHub Releases](https://github.com/Tproplay/Magnetar-Client/releases) page of the specific version. Ensure your game version matches the release.
* **Manual audio downloads:** If Audio Addon fails to download soundtracks automatically, see the [Audio Addon Manual Download Guide in FAQ.md](FAQ.md#2-audio-addon-is-showing-an-error-or-fails-to-download-the-soundtrack-automatically-where-can-i-download-audio-files-manually).
