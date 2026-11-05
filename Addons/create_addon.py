#!/usr/bin/env python3
"""
Magnetar Client Addon Project Generator
Scaffolds production-ready Magnetar Client addon projects with dual
MelonLoader & BepInEx support, safe Harmony patching, and reference management.

# Supported Templates:
#   1. General     - Full addon: Main (with IMagnetarAddon & OnLoad patching) + Module + HUD + Patch
#   2. Module      - Feature addon: Module (constructor patching) + Patch file (no IMagnetarAddon)
#   3. HUD Element - Display widget: HUD (constructor patching) + Patch file (no IMagnetarAddon)
"""

import argparse
import datetime
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

# Import the reference updater logic
from update_references import (
    DEFAULT_BRANCH,
    update_references,
    find_workspace_root,
)


# -----------------------------------------------------------------------------
# Name Utilities
# -----------------------------------------------------------------------------

def sanitize_identifier(name: str) -> str:
    """Converts a user-friendly name into a valid C# identifier (e.g. 'Radar ESP' -> 'RadarESP')."""
    clean = re.sub(r"[^a-zA-Z0-9_]+", " ", name)
    parts = clean.strip().split()
    if not parts:
        return "CustomAddon"
    safe = "".join(p.capitalize() if not p.isupper() else p for p in parts)
    if safe[0].isdigit():
        safe = "Addon" + safe
    return safe


def get_default_author() -> str:
    """Tries to get the git author name, falling back to system username."""
    try:
        out = subprocess.check_output(["git", "config", "user.name"], text=True, stderr=subprocess.DEVNULL)
        if out.strip():
            return out.strip()
    except Exception:
        pass
    return os.environ.get("USERNAME") or os.environ.get("USER") or "Your Name"


# -----------------------------------------------------------------------------
# File Templates
# -----------------------------------------------------------------------------

SOLUTION_TEMPLATE = """<Solution>
  <Configurations>
    <BuildType Name="BepInEx" />
    <BuildType Name="MelonLoader" />
  </Configurations>
  <Project Path="Project/{safe_name}.csproj" />
</Solution>
"""

CSPROJ_TEMPLATE = """<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <RootNamespace>{safe_name}</RootNamespace>
        <AssemblyName>{safe_name}</AssemblyName>
        <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
        <Configurations>MelonLoader;BepInEx</Configurations>
        <RefDir>$(MSBuildThisFileDirectory)..\\..\\..\\References</RefDir>
        <DebugType>none</DebugType>
        <DebugSymbols>false</DebugSymbols>
        <GenerateDependencyFile>false</GenerateDependencyFile>
    </PropertyGroup>

    <ItemDefinitionGroup>
        <Reference>
            <Private>false</Private>
        </Reference>
    </ItemDefinitionGroup>

    <!-- Loader Target Constants & Output Paths -->
    <PropertyGroup Condition="$(Configuration.Contains('MELON')) or $(Configuration.Contains('Melon'))">
        <OutputPath>bin\\$(Configuration)\\</OutputPath>
        <DefineConstants>$(DefineConstants);MELONLOADER</DefineConstants>
    </PropertyGroup>

    <PropertyGroup Condition="$(Configuration.Contains('BepInEx')) or $(Configuration.Contains('BEPINEX'))">
        <OutputPath>bin\\$(Configuration)\\</OutputPath>
        <DefineConstants>$(DefineConstants);BEPINEX</DefineConstants>
    </PropertyGroup>

    <!-- Shared Core References -->
    <ItemGroup>
        <Reference Include="0Harmony">
            <HintPath Condition="Exists('$(RefDir)\\Melonloader\\0Harmony.dll')">$(RefDir)\\Melonloader\\0Harmony.dll</HintPath>
            <HintPath Condition="!Exists('$(RefDir)\\Melonloader\\0Harmony.dll')">$(RefDir)\\Bepinex\\0Harmony.dll</HintPath>
        </Reference>
        <Reference Include="Magnetar Client">
            <HintPath>$(RefDir)\\Magnetar Client.dll</HintPath>
        </Reference>
    </ItemGroup>

    <!-- MelonLoader Engine References -->
    <ItemGroup Condition="$(Configuration.Contains('MELON')) or $(Configuration.Contains('Melon'))">
        <Reference Include="MelonLoader">
            <HintPath>$(RefDir)\\Melonloader\\MelonLoader.dll</HintPath>
        </Reference>
        <Reference Include="Il2CppInterop.Common">
            <HintPath>$(RefDir)\\Melonloader\\Il2CppInterop.Common.dll</HintPath>
        </Reference>
        <Reference Include="Il2CppInterop.Runtime">
            <HintPath>$(RefDir)\\Melonloader\\Il2CppInterop.Runtime.dll</HintPath>
        </Reference>
        <Reference Include="Assembly-CSharp">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\Assembly-CSharp.dll</HintPath>
        </Reference>
        <Reference Include="Il2Cppmscorlib">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\Il2Cppmscorlib.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\UnityEngine.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine.CoreModule">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\UnityEngine.CoreModule.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine.IMGUIModule">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\UnityEngine.IMGUIModule.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine.UI">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\UnityEngine.UI.dll</HintPath>
        </Reference>
        <Reference Include="Unity.TextMeshPro">
            <HintPath>$(RefDir)\\Melonloader\\Il2Cpp\\Unity.TextMeshPro.dll</HintPath>
        </Reference>
    </ItemGroup>

    <!-- BepInEx Engine References -->
    <ItemGroup Condition="$(Configuration.Contains('BepInEx')) or $(Configuration.Contains('BEPINEX'))">
        <Reference Include="BepInEx.Core">
            <HintPath>$(RefDir)\\Bepinex\\BepInEx.Core.dll</HintPath>
        </Reference>
        <Reference Include="BepInEx.Unity.IL2CPP">
            <HintPath>$(RefDir)\\Bepinex\\BepInEx.Unity.IL2CPP.dll</HintPath>
        </Reference>
        <Reference Include="Il2CppInterop.Runtime">
            <HintPath>$(RefDir)\\Bepinex\\Il2CppInterop.Runtime.dll</HintPath>
        </Reference>
        <Reference Include="Assembly-CSharp">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\Assembly-CSharp.dll</HintPath>
        </Reference>
        <Reference Include="Il2Cppmscorlib">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\Il2Cppmscorlib.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\UnityEngine.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine.CoreModule">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\UnityEngine.CoreModule.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine.IMGUIModule">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\UnityEngine.IMGUIModule.dll</HintPath>
        </Reference>
        <Reference Include="UnityEngine.UI">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\UnityEngine.UI.dll</HintPath>
        </Reference>
        <Reference Include="Unity.TextMeshPro">
            <HintPath>$(RefDir)\\Bepinex\\Il2Cpp\\Unity.TextMeshPro.dll</HintPath>
        </Reference>
    </ItemGroup>

</Project>
"""

ASSEMBLY_INFO_TEMPLATE = """using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("{display_name}")]
[assembly: AssemblyDescription("{display_name} - Magnetar Client Addon")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("{display_name}")]
[assembly: AssemblyCopyright("Copyright © {year}")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
"""

# Main.cs for General Template (Harmony patching initialized in OnLoad)
MAIN_CS_GENERAL_TEMPLATE = """using System.Reflection;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;

namespace {safe_name};

/// <summary>
/// Main addon lifecycle entrypoint discovered and loaded by Magnetar Client.
/// </summary>
public class Main : IMagnetarAddon
{{
    public static Main Instance {{ get; private set; }}

    public string Name => "{display_name}";
    public string Version => "1.0.0";
    public string Author => "{author}";

    public Main()
    {{
        Instance = this;
    }}

    public void OnLoad()
    {{
        // Patching initialized in OnLoad
        Actions.OnInitializeHarmony += SafePatchAll;
        Magnetar_Logger.DebugLogger.Msg($"[{{Name}}] Loaded");
    }}

    private void SafePatchAll()
    {{
        var patchInfo = HarmonyManager.HarmonyPatchAll(Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {{
            MagnetarApi.LogWarning($"[{{Name}}] Failed to apply patch '{{failure.ClassName}}': {{failure.Exception.Message}}");
        }}

        MagnetarApi.LogMsg($"[{{Name}}] Harmony initialized: {{patchInfo.SuccessCount}} class(es) patched.");
    }}

    public void OnUnload()
    {{
        Actions.OnInitializeHarmony -= SafePatchAll;
    }}
}}
"""


# Module with Harmony patching inside the constructor (for 'module' template)
MODULE_CS_WITH_PATCHING_TEMPLATE = """using Magnetar_Client.Api;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;

namespace {safe_name};

/// <summary>
/// Magnetar module showing settings, UI grouping, and lifecycle hooks.
/// Harmony patching is initialized in the constructor.
/// </summary>
public class {module_class_name} : Module
{{
    // Mod Info
    public override string Name {{ get; set; }} = "{display_name} Feature";
    public override string Description {{ get; set; }} = "Demonstrates settings and lifecycle callbacks.";
    public override string SearchHints {{ get; set; }} = "{safe_name_lower} addon feature";
    public override string Author {{ get; set; }} = "{author}";
    public override ModuleCategory Category {{ get; set; }} = ModuleCategory.Addon;

    public override bool EnableInVanillaMode {{ get; set; }} = true;

    // Mod Data / Settings
    public BoolSetting EnableFeature;
    public FloatSetting Multiplier;
    public StringSetting CustomText;

    public static {module_class_name} Instance {{ get; private set; }}

    public {module_class_name}()
    {{
        Instance = this;

        // Patching in Module constructor
        SafePatchAll();

        CreateCategory("General");

        EnableFeature = new BoolSetting("Enable Sub-feature", true);
        Multiplier = new FloatSetting("Multiplier", 1.0f, 10.0f, 2.0f, 1);
        CustomText = new StringSetting("Custom Message", "Hello from {display_name}!");

        AddSettings(EnableFeature, Multiplier, CustomText);
        EndCategory();
    }}

    private void SafePatchAll()
    {{
        if (Magnetar_Client.Core.Main.HarmonyInstance == null)
        {{
            Actions.OnInitializeHarmony += SafePatchAll;
            return;
        }}

        var patchInfo = HarmonyManager.HarmonyPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {{
            MagnetarApi.LogWarning($"[{{Name}}] Failed to apply patch '{{failure.ClassName}}': {{failure.Exception.Message}}");
        }}

        MagnetarApi.LogMsg($"[{{Name}}] Harmony initialized: {{patchInfo.SuccessCount}} class(es) patched.");
    }}

    // Mod Logic

    public override void OnUpdateActive()
    {{
        // Logic running every frame while this module is active
    }}

    public override void OnEnable()
    {{
        Magnetar_Logger.DebugLogger.Msg($"[{{Name}}] Enabled.");
    }}

    public override void OnDisable()
    {{
        Magnetar_Logger.DebugLogger.Msg($"[{{Name}}] Disabled.");
    }}
}}
"""

# Module without constructor patching (used in 'general' template where Main.OnLoad handles patching)
MODULE_CS_SIMPLE_TEMPLATE = """using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;

namespace {safe_name};

/// <summary>
/// Magnetar module showing settings, UI grouping, and lifecycle hooks.
/// Any class extending Module is automatically discovered and registered by Magnetar Client.
/// </summary>
public class {module_class_name} : Module
{{
    // Mod Info
    public override string Name {{ get; set; }} = "{display_name} Feature";
    public override string Description {{ get; set; }} = "Demonstrates settings and lifecycle callbacks.";
    public override string SearchHints {{ get; set; }} = "{safe_name_lower} addon feature";
    public override string Author {{ get; set; }} = "{author}";
    public override ModuleCategory Category {{ get; set; }} = ModuleCategory.Addon;

    public override bool EnableInVanillaMode {{ get; set; }} = true;

    // Mod Data / Settings
    public BoolSetting EnableFeature;
    public FloatSetting Multiplier;
    public StringSetting CustomText;

    public static {module_class_name} Instance {{ get; private set; }}

    public {module_class_name}()
    {{
        Instance = this;

        CreateCategory("General");

        EnableFeature = new BoolSetting("Enable Sub-feature", true);
        Multiplier = new FloatSetting("Multiplier", 1.0f, 10.0f, 2.0f, 1);
        CustomText = new StringSetting("Custom Message", "Hello from {display_name}!");

        AddSettings(EnableFeature, Multiplier, CustomText);
        EndCategory();
    }}

    // Mod Logic

    public override void OnUpdateActive()
    {{
        // Logic running every frame while this module is active
    }}

    public override void OnEnable()
    {{
        Magnetar_Logger.DebugLogger.Msg($"[{{Name}}] Enabled.");
    }}

    public override void OnDisable()
    {{
        Magnetar_Logger.DebugLogger.Msg($"[{{Name}}] Disabled.");
    }}
}}
"""

# HUD Element with Harmony patching inside the constructor (for 'hud' template)
HUD_CS_WITH_PATCHING_TEMPLATE = """using Magnetar_Client.Api;
using Magnetar_Client.HUDElements;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using UnityEngine;

namespace {safe_name};

/// <summary>
/// On-screen HUD element discovered and rendered by Magnetar Client HUD Manager.
/// Harmony patching is initialized in the constructor.
/// </summary>
public class {hud_class_name} : HudElement
{{
    private string _displayText = "{display_name}: <color=lime>Active</color>";

    public {hud_class_name}() : base("{display_name} HUD", HudElement.NewRect(180, 24))
    {{
        UpdateInterval = 1.0f;

        // Patching inside HUD element constructor
        SafePatchAll();
    }}

    private void SafePatchAll()
    {{
        if (Magnetar_Client.Core.Main.HarmonyInstance == null)
        {{
            Actions.OnInitializeHarmony += SafePatchAll;
            return;
        }}

        var patchInfo = HarmonyManager.HarmonyPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), Magnetar_Client.Core.Main.HarmonyInstance);

        foreach (var failure in patchInfo.Failures)
        {{
            MagnetarApi.LogWarning($"[{{Name}}] Failed to apply patch '{{failure.ClassName}}': {{failure.Exception.Message}}");
        }}

        MagnetarApi.LogMsg($"[{{Name}}] Harmony initialized: {{patchInfo.SuccessCount}} class(es) patched.");
    }}

    protected override void DrawContent(float width, float height)
    {{
        GUI.Label(new Rect(5, 4, width - 10, height - 10), _displayText, ThemeManager.HUDElementStyle);
    }}

    public override void OnUpdateActive()
    {{
        int frameCount = Time.frameCount;
        _displayText = $"{display_name}: <color=lime>{{Magnetar_Client.Utils.Maths.FormatInternational(frameCount)}}</color>";

        // Dynamically adjust window width to fit the text with 10px padding
        AdjustWidthToText(_displayText, ThemeManager.HUDElementStyle, 10f);
    }}

    public override void OnEnable()
    {{
        AdjustWidthToText(_displayText, ThemeManager.HUDElementStyle, 10f);
    }}
}}
"""

# HUD Element without constructor patching (used in 'general' template where Main.OnLoad handles patching)
HUD_CS_SIMPLE_TEMPLATE = """using Magnetar_Client.HUDElements;
using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace {safe_name};

/// <summary>
/// On-screen HUD element discovered and rendered by Magnetar Client HUD Manager.
/// </summary>
public class {hud_class_name} : HudElement
{{
    private string _displayText = "{display_name}: <color=lime>Active</color>";

    public {hud_class_name}() : base("{display_name} HUD", HudElement.NewRect(180, 24))
    {{
        UpdateInterval = 1.0f;
    }}

    protected override void DrawContent(float width, float height)
    {{
        GUI.Label(new Rect(5, 4, width - 10, height - 10), _displayText, ThemeManager.HUDElementStyle);
    }}

    public override void OnUpdateActive()
    {{
        int frameCount = Time.frameCount;
        _displayText = $"{display_name}: <color=lime>{{Magnetar_Client.Utils.Maths.FormatInternational(frameCount)}}</color>";

        // Dynamically adjust window width to fit the text with 10px padding
        AdjustWidthToText(_displayText, ThemeManager.HUDElementStyle, 10f);
    }}

    public override void OnEnable()
    {{
        AdjustWidthToText(_displayText, ThemeManager.HUDElementStyle, 10f);
    }}
}}
"""

# Harmony Patch file included with all templates
PATCH_CS_TEMPLATE = """using System;
using System.Reflection;
using HarmonyLib;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;

namespace {safe_name};

/// <summary>
/// Sample Harmony patch template demonstrating method hooking.
/// To activate: uncomment [HarmonyPatch] attributes and specify your target class and method.
/// </summary>
// [HarmonyPatch(typeof(TargetGameClass), nameof(TargetGameClass.TargetMethod))]
public static class {patch_class_name}
{{
    // Prefix: Runs BEFORE the original game method.
    // Return true to continue with original execution, or false to skip original method.
    // [HarmonyPrefix]
    // public static bool Prefix()
    // {{
    //     Magnetar_Logger.DebugLogger.Msg("[{display_name}] Prefix hook executed.");
    //     return true;
    // }}

    // Postfix: Runs AFTER the original game method completes.
    // [HarmonyPostfix]
    // public static void Postfix()
    // {{
    //     Magnetar_Logger.DebugLogger.Msg("[{display_name}] Postfix hook executed.");
    // }}
}}
"""

README_TEMPLATE = """# {display_name}

> Magnetar Client Addon developed by {author}.

---

## Project Structure

```
{display_name}/
├── {safe_name}.slnx              # Visual Studio Solution (MelonLoader & BepInEx)
├── update_references.py        # Helper to sync/download DLL references
├── README.md                   # This document
└── Project/
    ├── {safe_name}.csproj        # SDK Project file
    ├── Properties/
    │   └── AssemblyInfo.cs     # Assembly metadata
    ├── References/             # Engine and loader DLL dependencies
    └── Src/                    # C# Source code
```

---

## Building the Addon

Open `{safe_name}.slnx` in **Visual Studio 2022** or **JetBrains Rider**, select your desired configuration, and build.

Or use the .NET CLI:

```bash
# Build for BepInEx (IL2CPP)
dotnet build "Project/{safe_name}.csproj" -c BepInEx

# Build for MelonLoader (IL2CPP)
dotnet build "Project/{safe_name}.csproj" -c MelonLoader
```

The output DLL will be placed in:
* **BepInEx**: `Project/bin/BepInEx/BepInEx/{safe_name}.dll`
* **MelonLoader**: `Project/bin/MelonLoader/{safe_name}.dll`

---

## Updating References

To download or update the latest references from GitHub at any time:

```bash
python update_references.py
```

To force download directly from GitHub:
```bash
python update_references.py --github
```
"""


# -----------------------------------------------------------------------------
# Generation Logic
# -----------------------------------------------------------------------------

def scaffold_addon(
    display_name: str,
    author: str,
    template: str,
    output_parent: Path,
    download_github: bool = False,
    branch: str = DEFAULT_BRANCH,
    skip_references: bool = False,
) -> Path:
    template = template.lower()
    if template not in ("general", "module", "hud"):
        template = "general"

    safe_name = sanitize_identifier(display_name)
    addon_dir = output_parent / display_name
    project_dir = addon_dir / "Project"
    src_dir = project_dir / "Src"
    props_dir = project_dir / "Properties"
    refs_dir = project_dir / "References"

    if addon_dir.exists() and any(addon_dir.iterdir()):
        raise FileExistsError(f"Directory already exists and is not empty: {addon_dir}")

    print(f"\n[+] Scaffolding '{display_name}' ({safe_name}) [Template: {template}] in {addon_dir}...")
    src_dir.mkdir(parents=True, exist_ok=True)
    props_dir.mkdir(parents=True, exist_ok=True)
    refs_dir.mkdir(parents=True, exist_ok=True)

    year = datetime.datetime.now().year
    module_class_name = f"{safe_name}Module"
    hud_class_name = f"{safe_name}HudElement"
    patch_class_name = f"{safe_name}Patch"

    # 1. Solution file
    slnx_path = addon_dir / f"{safe_name}.slnx"
    slnx_path.write_text(SOLUTION_TEMPLATE.format(safe_name=safe_name), encoding="utf-8")
    print(f"  [+] Created {slnx_path.name}")

    # 2. Csproj
    csproj_path = project_dir / f"{safe_name}.csproj"
    csproj_path.write_text(CSPROJ_TEMPLATE.format(safe_name=safe_name), encoding="utf-8")
    print(f"  [+] Created Project/{csproj_path.name}")

    # 3. AssemblyInfo.cs
    asm_info_path = props_dir / "AssemblyInfo.cs"
    asm_info_path.write_text(
        ASSEMBLY_INFO_TEMPLATE.format(
            display_name=display_name,
            safe_name=safe_name,
            year=year,
        ),
        encoding="utf-8",
    )
    print("  [+] Created Project/Properties/AssemblyInfo.cs")

    # 4. Main.cs (Only included in 'general' template with IMagnetarAddon)
    if template == "general":
        main_cs_path = src_dir / "Main.cs"
        main_cs_path.write_text(
            MAIN_CS_GENERAL_TEMPLATE.format(
                display_name=display_name,
                safe_name=safe_name,
                author=author,
            ),
            encoding="utf-8",
        )
        print("  [+] Created Project/Src/Main.cs (with IMagnetarAddon & OnLoad Harmony patching)")

    # 5. Module File
    if template == "general":
        module_path = src_dir / f"{module_class_name}.cs"
        module_path.write_text(
            MODULE_CS_SIMPLE_TEMPLATE.format(
                display_name=display_name,
                safe_name=safe_name,
                safe_name_lower=safe_name.lower(),
                author=author,
                module_class_name=module_class_name,
            ),
            encoding="utf-8",
        )
        print(f"  [+] Created Project/Src/{module_path.name}")
    elif template == "module":
        module_path = src_dir / f"{module_class_name}.cs"
        module_path.write_text(
            MODULE_CS_WITH_PATCHING_TEMPLATE.format(
                display_name=display_name,
                safe_name=safe_name,
                safe_name_lower=safe_name.lower(),
                author=author,
                module_class_name=module_class_name,
            ),
            encoding="utf-8",
        )
        print(f"  [+] Created Project/Src/{module_path.name} (with constructor Harmony patching)")

    # 6. HUD Element File
    if template == "general":
        hud_path = src_dir / f"{hud_class_name}.cs"
        hud_path.write_text(
            HUD_CS_SIMPLE_TEMPLATE.format(
                display_name=display_name,
                safe_name=safe_name,
                hud_class_name=hud_class_name,
            ),
            encoding="utf-8",
        )
        print(f"  [+] Created Project/Src/{hud_path.name}")
    elif template == "hud":
        hud_path = src_dir / f"{hud_class_name}.cs"
        hud_path.write_text(
            HUD_CS_WITH_PATCHING_TEMPLATE.format(
                display_name=display_name,
                safe_name=safe_name,
                hud_class_name=hud_class_name,
            ),
            encoding="utf-8",
        )
        print(f"  [+] Created Project/Src/{hud_path.name} (with constructor Harmony patching)")

    # 7. Harmony Patch File (Included with ALL templates)
    patch_path = src_dir / f"{patch_class_name}.cs"
    patch_path.write_text(
        PATCH_CS_TEMPLATE.format(
            display_name=display_name,
            safe_name=safe_name,
            patch_class_name=patch_class_name,
        ),
        encoding="utf-8",
    )
    print(f"  [+] Created Project/Src/{patch_path.name} (Harmony patch hooks)")

    # 8. Copy update_references.py into the addon directory
    updater_source = Path(__file__).resolve().parent / "update_references.py"
    updater_dest = addon_dir / "update_references.py"
    if updater_source.is_file():
        shutil.copy2(updater_source, updater_dest)
        print("  [+] Created update_references.py")

    # 9. README.md
    readme_path = addon_dir / "README.md"
    readme_path.write_text(
        README_TEMPLATE.format(
            display_name=display_name,
            safe_name=safe_name,
            author=author,
        ),
        encoding="utf-8",
    )
    print("  [+] Created README.md")

    # 10. Populate References
    if not skip_references:
        print("\n[*] Populating reference DLLs...")
        update_references(refs_dir, force_github=download_github, branch=branch)

    return addon_dir


# -----------------------------------------------------------------------------
# Main & CLI
# -----------------------------------------------------------------------------

def interactive_prompt() -> tuple[str, str, str, bool]:
    print("=" * 60)
    print("        Magnetar Client — Addon Generator")
    print("=" * 60)

    while True:
        display_name = input("Addon Name (e.g. 'Radar ESP'): ").strip()
        if display_name:
            break
        print("Addon name cannot be empty.")

    default_author = get_default_author()
    author_input = input(f"Author [{default_author}]: ").strip()
    author = author_input if author_input else default_author

    print("\nSelect Template:")
    print("  [1] General     (Full addon: IMagnetarAddon + Module + HUD + Patch file) [Default]")
    print("  [2] Module      (Module with constructor patching + Patch file, no IMagnetarAddon)")
    print("  [3] HUD Element (HUD Element with constructor patching + Patch file, no IMagnetarAddon)")
    choice = input("Choose template [1-3, default: 1]: ").strip()

    template_map = {"1": "general", "2": "module", "3": "hud"}
    template = template_map.get(choice, "general")

    dl_input = input("Download references directly from GitHub? (y/N): ").strip().lower()
    download_github = dl_input in ("y", "yes")

    return display_name, author, template, download_github


def normalize_template_arg(arg: str | None) -> str | None:
    if not arg:
        return None
    val = arg.lower().strip()
    if val in ("general", "standard", "all"):
        return "general"
    if val in ("module", "mod"):
        return "module"
    if val in ("hud", "hud element", "hud_element", "hudelement"):
        return "hud"
    return "general"


def main():
    parser = argparse.ArgumentParser(description="Scaffold a new Magnetar Client addon project.")
    parser.add_argument("name", nargs="?", default=None, help="Name of the addon (e.g. 'Radar ESP')")
    parser.add_argument("-t", "--template", default=None, help="Template flavor: general, module, or hud (default: general)")
    parser.add_argument("-a", "--author", default=None, help="Addon author name")
    parser.add_argument("-o", "--output", default=None, help="Output parent directory (defaults to Addons/)")
    parser.add_argument("-g", "--github", action="store_true", help="Download references from GitHub instead of local copy")
    parser.add_argument("-b", "--branch", default=DEFAULT_BRANCH, help=f"GitHub branch to download references from (default: {DEFAULT_BRANCH})")
    parser.add_argument("--no-refs", action="store_true", help="Skip reference populating")
    args = parser.parse_args()

    script_dir = Path(__file__).resolve().parent
    workspace_root = find_workspace_root() or script_dir.parent
    output_parent = Path(args.output).resolve() if args.output else (workspace_root / "Addons")

    if args.name:
        display_name = args.name
        author = args.author or get_default_author()
        template = normalize_template_arg(args.template) or "general"
        download_github = args.github
    else:
        display_name, author, template, download_github = interactive_prompt()
        if args.github:
            download_github = True

    try:
        addon_dir = scaffold_addon(
            display_name=display_name,
            author=author,
            template=template,
            output_parent=output_parent,
            download_github=download_github,
            branch=args.branch,
            skip_references=args.no_refs,
        )
        print("\n" + "=" * 60)
        print(f"SUCCESS: Created addon at: {addon_dir}")
        print(f"Open '{addon_dir / (sanitize_identifier(display_name) + '.slnx')}' to start coding!")
        print("=" * 60)
    except Exception as e:
        print(f"\n[!] Error scaffolding project: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
