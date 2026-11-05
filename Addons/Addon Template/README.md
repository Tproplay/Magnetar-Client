# Magnetar Client — Addon Template & Development Guide

> A production-ready quick-start template and development guide for creating addons for **Magnetar Client** with dual **MelonLoader** and **BepInEx** support.

---

## Architecture Overview

```
Addon Template/
├── Addon Template.slnx               # Visual Studio Solution file (MelonLoader & BepInEx)
├── README.md                         # This developer guide
└── Project/
    ├── Addon Template.csproj         # SDK project file with dual loader support
    ├── Properties/
    │   └── AssemblyInfo.cs           # Assembly metadata & attributes
    ├── References/                   # Pre-populated BepInEx, MelonLoader, & Client DLLs
    └── Src/
        ├── Main.cs                   # IMagnetarAddon entrypoint & Harmony initialization
        ├── SampleModule.cs           # Example Module with settings & lifecycle hooks
        ├── SampleHudElement.cs       # Example on-screen HUD window element
        └── SamplePatch.cs            # Example Harmony hook template
```

---

## Addon Types & Lifecycle

Magnetar Client dynamically loads **all `.dll` assemblies** placed inside the game's `Addons/` folder. It discovers components via reflection:

### 1. General Addons (`IMagnetarAddon`)
* **Discovery**: Discovered during `AddonManager.InitAddons()`.
* **Purpose**: Used for addons that need explicit initialization lifecycle hooks (`OnLoad()`, `OnUnload()`), action subscriptions (`Actions.OnInitializeHarmony`, `Actions.OnLateInitializeCore`), or custom service registration.
* **Harmony Patching**: Typically initializes assembly-wide patches during `OnLoad()` using `HarmonyManager.HarmonyPatchAll()`.

### 2. Standalone Modules (`Module`)
* **Discovery**: Discovered during `AddonManager.InitModules()`.
* **No `IMagnetarAddon` Required**: If your addon only provides feature modules, it does **not** need `IMagnetarAddon` or `Main.cs`. Magnetar Client instantiates all `Module` subclasses automatically.
* **Harmony Patching**: In standalone module addons, safe Harmony patching can be initialized directly inside the module's constructor.
* **Settings & UI**: Provides built-in setting types (`BoolSetting`, `FloatSetting`, `IntSetting`, `StringSetting`, `EnumSetting`, `KeyBind`) and category grouping (`CreateCategory()`, `AddSettings()`, `EndCategory()`).
* **Lifecycle**: Implement `OnUpdateActive()` for frame logic, `OnEnable()` / `OnDisable()` for state transitions.

### 3. Standalone HUD Elements (`HudElement`)
* **Discovery**: Discovered during `AddonManager.InitHUDElements()`.
* **No `IMagnetarAddon` Required**: Standalone HUD widgets do not require `IMagnetarAddon` or `Main.cs`.
* **Harmony Patching**: Can be initialized directly inside the HUD element constructor.
* **Rendering & Auto-Sizing**: Override `DrawContent(width, height)` with IMGUI controls, set `UpdateInterval`, and implement `OnUpdateActive()` to update dynamic text with `AdjustWidthToText()`.

### 4. Harmony Patches (`[HarmonyPatch]`)
* Classes decorated with `[HarmonyPatch]` provide prefix/postfix hooks into the game's methods.
* Applied safely via `HarmonyManager.HarmonyPatchAll(assembly, harmonyInstance)`, which isolates failing patches without crashing the client.

---

## Generating New Addons (`create_addon.py`)

Rather than manually copying and renaming project files, use the generator script located in the `Addons/` directory:

### Interactive Mode
```bash
python Addons/create_addon.py
```
This prompts for:
* **Addon Name**: (e.g. `Audio Addon` $\rightarrow$ creates `AudioAddon.slnx`, `AudioAddon.csproj`, etc.)
* **Author Name**: (auto-detected from git or system username)
* **Template Flavor**:
  * `[1] General` — Full addon: `Main.cs` (with `IMagnetarAddon` & `OnLoad` patching) + `Module` + `HUD` + `Patch`
  * `[2] Module` — Standalone feature: `Module` (with constructor patching) + `Patch` *(no `IMagnetarAddon`)*
  * `[3] HUD Element` — Standalone display widget: `HUD` (with constructor patching) + `Patch` *(no `IMagnetarAddon`)*
* **Reference Source**: Copy from local `Addon Template` (instant) or download from GitHub.

### CLI One-Liners
```bash
# General addon (full lifecycle, module, HUD, and patch)
python Addons/create_addon.py "Radar ESP" --template general

# Module-only addon (no IMagnetarAddon, patching in module constructor)
python Addons/create_addon.py "Audio Addon" --template module

# HUD-only addon (no IMagnetarAddon, patching in HUD constructor)
python Addons/create_addon.py "FPS Counter" --template hud

# Download references directly from GitHub
python Addons/create_addon.py "Speed Hack" --template module --github
```

---

## Reference Management & Updater (`update_references.py`)

Each addon project includes local references under `Project/References/` so it compiles completely out-of-the-box.

Every generated addon also includes `update_references.py` at its root:

```bash
# Sync all 45 reference DLLs from local Addon Template
python update_references.py

# Force download latest references directly from GitHub (Tproplay/Magnetar-Client)
python update_references.py --github

# Download from a specific GitHub branch
python update_references.py --github --branch devlop
```

---

## Build Configurations

The project includes two preconfigured build configurations:

| Configuration | Mod Loader | Output Directory |
|---|---|---|
| `MelonLoader` | MelonLoader (IL2CPP) | `Project/bin/MelonLoader/<Name>.dll` |
| `BepInEx` | BepInEx (IL2CPP) | `Project/bin/BepInEx/<Name>.dll` |

### Building from Visual Studio / IDE
Open the `<Name>.slnx` solution file in **Visual Studio 2022** or **JetBrains Rider**, select `MelonLoader` or `BepInEx` from the configuration dropdown, and build.

### Building via CLI
```bash
# Build BepInEx target
dotnet build "Project/Addon Template.csproj" -c BepInEx

# Build MelonLoader target
dotnet build "Project/Addon Template.csproj" -c MelonLoader
```

---

## Preprocessor Symbols

When writing loader-specific code (e.g. `Il2Cpp` namespaces for MelonLoader vs standard namespaces for BepInEx):

```csharp
#if MELONLOADER
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX
using TMPro;
#endif
```

---

## Installation & In-Game Testing

Copy the compiled `.dll` from the output directory into your game's Magnetar addons folder:

* **MelonLoader**: `<GameRoot>/Mods/Magnetar Data/Addons/`
* **BepInEx**: `<GameRoot>/BepInEx/plugins/Magnetar Data/Addons/`

Launch the game; your addon, modules, and HUD elements will appear automatically in the Magnetar Client UI.
