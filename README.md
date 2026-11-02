<div align="center">
  <img src="assets/MagnetarClientLogo.png" width="170" alt="Magnetar Client Logo">
  <h1 class="smooth-gradient">Magnetar Client</h1>
  <p><b>A high-performance utility client for Plants Vs. Zombies Fusion.</b></p>

  <img src="https://img.shields.io/github/v/release/Tproplay/Magnetar-Client?style=flat-square&color=blue" alt="Latest Release">
  <img src="https://img.shields.io/github/downloads/Tproplay/Magnetar-Client/total?style=flat-square&color=success" alt="Downloads">
  
</div>

<br/>

**Magnetar Client** is a feature-rich mod for *Plants Vs. Zombies Fusion* designed to elevate your gameplay experience. It offers comprehensive quality-of-life (QoL) improvements, advanced game modifications, and a fully customizable UI display—all meticulously optimized to maintain peak performance.

## Features

* #### Quality-of-Life features such as:
  * Mute sounds, hide/change projectiles and metal objects
  * Better health display
  * Alter projectile sizes
  * Most of these are used to remove annoyances with the game while not changing the game itself.
               
* You can customize HUD elements and the user interface freely.
* Very little performance impact.
* Integration with NEF (Not Enough Fusions) to view all available fusion recipes.
* #### Cheats such as: 
  * Changing sun, money, points, lawnmowers, rerolls, odyssey modifiers, seed packets.
  * Change almost any cooldown (glove, seed packet, hammer, wheelbarrow).
  * Change plant speed and projectiles, homing bullets. Force giftbox rng.
  * Alter zombie hp, speed and waves. Clamp zombies.
  * Kill/hypnotize all zombies, clear lawn, save setup for later use, illegal placements.
---
## 🎮 Controls

| Action | Keybind |
| :--- | :--- |
| **Show/Hide Mod Menu** | <kbd>Right Shift</kbd> |
| **Open Module Settings** | <kbd>Right Click</kbd> |
| **Open Search Bar** | <kbd>Enter</kbd> |

---

## Installation Guide

Choose your preferred mod loader below and follow the respective instructions.

### 🍉 MelonLoader

> **Note:** Ensure you have [MelonLoader](https://melonwiki.xyz/) installed before proceeding.

1. **Download** the [latest MelonLoader release](https://github.com/Tproplay/Magnetar-Client/releases) `.zip` file.
2. **Extract** the downloaded archive to a convenient location.
3. **Copy** the contents of the `Mods` folder into `[Your Game Folder]\Mods`.
4. **Copy** the contents of the `UserLibs` folder into `[Your Game Folder]\UserLibs`.
5. **Launch** your game to activate the client!
> **Optional:** Remove the `Blooms_QOL.dll` if you are installing on Multi-lang version, as it might conflict with it.

**Folder Structure**
After installing the files, your game directory structure should look like this:

```text
Game-Files/
├── MelonLoader/
├── Mods/                    <-- (Paste Mods file in here)
│   ├── Magnetar Data/
│   └── Magnetar Client.dll
├── PlantsVsZombiesRH_Data/
├── UserData/
├── UserLibs/                <-- (Paste UserLibs file in here)
│   └── DiscordRPC.dll
├── baselib.dll
├── GameAssembly.dll
├── PlantsVsZombiesRH.exe    <-- (Your game executable)
├── UnityCrashHandler64.exe
├── UnityPlayer.dll
└── version.dll
```

### BepInEx

> **Note:** Ensure you have [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) installed before proceeding.

1. **Download** the [latest Bepinex release](https://github.com/Tproplay/Magnetar-Client/releases) `.zip` file.
2. **Extract** the downloaded archive to a convenient location.
3. **Copy** the contents of the `BepInEx\plugins` folder into `[Your Game Folder]\BepInEx\plugins`.
4. **Launch** your game to activate the client!

### 📂 Folder Structure
After installing the files, your game directory structure should look like this:

    Game-Files/
    ├── BepInEx/
    │   ├── core/
    │   └── plugins/                <-- (Paste plugins file in here)
    │       ├── Magnetar Data/
    │       ├── DiscordRPC.dll
    │       ├── Magnetar Client.dll
    │       └── Newtonsoft.Json.dll
    ├── dotnet/
    ├── PlantsVsZombiesRH_Data/
    ├── .doorstop_version
    ├── baselib.dll
    ├── changelog.txt
    ├── doorstop_config.ini
    ├── GameAssembly.dll
    ├── PlantsVsZombiesRH.exe       <-- (Your game executable)
    ├── UnityCrashHandler64.exe
    ├── UnityPlayer.dll
    └── winhttp.dll


---

### 📱 Android (PVZRH Launcher)

1. **Download & Install** the [latest PVZRH Android Launcher](https://github.com/ModPVZRH/PVZRH.Android.Launcher/releases). 
2. **Open the launcher** once to generate its system folders, then close it.
3. **Download** the [latest Android release](https://github.com/Tproplay/Magnetar-Client/releases) `.zip` file.
6. **Open** the PVZRH Launcher again.
7. **Navigate** to the **Modpacks** tab (the 2nd icon at the bottom of the screen).
8. **Tap** the **Curled Page icon** in the top-right corner.
9. **Select** the `MagnetarClient-version-Android.zip` file to import it.

---

## ❓ Frequently Asked Questions

>**Q: My plants are invincible even though I have God Mode Plants turned off, or they are stacking without Plant Anywhere enabled. How do I fix this?** <br>
**A:** Ensure you are using the correct version of the mod. Game versions must exactly match the mod versions (e.g., 3.6.x mod versions are only compatible with game versions 3.6.x).

---

## Addons

> [**Simple Spawner**](https://github.com/Tproplay/Simple-Spawner)

---

## 👥 Credits

* 👑 **Tproplay** — Main Developer
* 🇻🇳 **Kyzerin9999** — Playtester & Vietnamese Translator
* 🇪🇸 **UŁTRA_Badlander 400** - Spanish Translator
* 🧪 **Qwwwww** — Playtester
* 🧪 **Gosu** — Playtester
* 🧪 **D2013I** — Playtester
* 🧪 **gaotmaster** — Playtester
* 🦇 **The Dark Knight** — Playtester
* 🧪 **Lêthāl_₵Ø₦QɄɆⱤɆⱤ** - Playtester
* 🧪 **Arch Chomp ( Sin of Chomping )** - Playtester
---

## 🙏 Special Thanks

* **Infinite75** & **CareFreeSong**: For their incredible work on [PVZRHTools](https://github.com/CarefreeSongs712/PVZRHTools/).
* **Blooms Community**: Join the discussion on their [Discord Server](https://discord.gg/DPAC5ZVJ8T).
* **HayashiUme** & **LibraHp**: For their [android Bepinex launcher](https://github.com/ModPVZRH/PVZRH.Android.Launcher).
