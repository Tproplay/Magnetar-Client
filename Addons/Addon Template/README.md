# Magnetar Client — Addon Template

> A clean, production-ready quick-start template for creating new addons for **Magnetar Client** with dual **MelonLoader** and **BepInEx** support.

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
    └── Src/
        ├── Main.cs                   # IMagnetarAddon entrypoint & Harmony initialization
        ├── SampleModule.cs           # Example Module with settings & lifecycle hooks
        ├── SampleHudElement.cs       # Example on-screen HUD window element
        └── SamplePatch.cs            # Example Harmony hook with safety guards
```

---

## Core Components

### 1. The Entrypoint (`Src/Main.cs`)
Every addon implements [`IMagnetarAddon`](../Magnetar%20Client/src/Api/IMagnetarAddon.cs). When Magnetar Client starts, it scans the `Addons/` directory, discovers all `IMagnetarAddon` implementations, and invokes their lifecycle events:
* **`OnLoad()`**: Runs right after your assembly is loaded. Use this to subscribe to client actions (e.g., `Actions.OnInitializeHarmony`, `Actions.OnLateInitializeCore`).
* **`SafePatchAll()`**: Uses Magnetar Client's built-in `HarmonyManager.HarmonyPatchAll` to apply all patches in the assembly, isolating errors and logging diagnostics without crashing the client.
* **`OnUnload()`**: Clean up resources, event handlers, and active coroutines.

### 2. Modules & Settings (`Src/SampleModule.cs`)
Subclasses of `Module` are automatically detected and registered into the Magnetar in-game menu:
* **Settings**: Use built-in types like `BoolSetting`, `FloatSetting`, `IntSetting`, `StringSetting`, `EnumSetting`, and `KeyBind`.
* **Grouping**: Use `CreateCategory("Name")`, `AddSettings(...)`, and `EndCategory()` to organize options into clean collapsible sections.
* **Lifecycle**: Implement `OnUpdate()` for per-frame logic, `OnGUI()` for IMGUI overlays, and `OnEnable()` / `OnDisable()` for state transitions.

### 3. HUD Elements (`Src/SampleHudElement.cs`)
Subclasses of `HudElement` are automatically discovered and registered into the client's HUD Manager:
* **Window Configuration**: Provide a default name and `NewRect(width, height)`.
* **Rendering**: Implement `DrawContent(width, height)` with IMGUI labels/buttons.
* **Updates**: Set `UpdateInterval` (seconds) and implement `OnUpdateActive()` to update dynamic text and auto-resize with `AdjustWidthToText()`.

### 4. Harmony Patches (`Src/SamplePatch.cs`)
Hook into game methods safely:
* Always check if your module is active: `if (!SampleModule.Instance.Active) return;`
* Always perform null-checks on `__instance`.
* Wrap non-trivial logic in `try/catch` and log via `Magnetar_Logger.DebugLogger` or `MagnetarApi.LogWarning`.

---

## Quick-Start Checklist: Creating a New Addon

1. **Copy the folder**:
   Duplicate `Addons/Addon Template/` and rename it to your addon's name (e.g. `Addons/MyAwesomeAddon/`).

2. **Rename project & solution files**:
   - `Addon Template.slnx` $\rightarrow$ `MyAwesomeAddon.slnx`
   - `Project/Addon Template.csproj` $\rightarrow$ `Project/MyAwesomeAddon.csproj`
   - Update the project path inside `MyAwesomeAddon.slnx`.

3. **Update `Project/MyAwesomeAddon.csproj`**:
   - `<RootNamespace>MyAwesomeAddon</RootNamespace>`
   - `<AssemblyName>MyAwesomeAddon</AssemblyName>`

4. **Update `Project/Properties/AssemblyInfo.cs`**:
   - Set `AssemblyTitle`, `AssemblyProduct`, and `AssemblyDescription`.

5. **Update `Project/Src/Main.cs`**:
   - Set `Name`, `Version`, and `Author`.

---

## Build Targets

The project includes 2 preconfigured build profiles:

| Configuration | Mod Loader | Output Directory |
|---|---|---|
| `MelonLoader` | MelonLoader (IL2CPP) | `bin\Melonloader\MelonLoader\` |
| `BepInEx` | BepInEx (IL2CPP) | `bin\BepInEx\BepInEx\` |

### CLI Build Commands

```bash
# Build BepInEx target
dotnet build "Project/MyAwesomeAddon.csproj" -c BepInEx

# Build MelonLoader target
dotnet build "Project/MyAwesomeAddon.csproj" -c MelonLoader
```

---

## Preprocessor Symbols

When writing platform-specific code (e.g. `Il2Cpp` namespaces for MelonLoader vs vanilla namespaces for BepInEx):

```csharp
#if MELONLOADER
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX
using TMPro;
#endif
```

---

## Installation & Testing

Copy the compiled `.dll` from the output directory into your game's Magnetar addons folder:
* **MelonLoader**: `<GameRoot>/Mods/Magnetar Data/Addons/`
* **BepInEx**: `<GameRoot>/BepInEx/plugins/Magnetar Data/Addons/`

Launch the game; your addon, modules, and HUD elements will appear automatically in the Magnetar Client UI.
