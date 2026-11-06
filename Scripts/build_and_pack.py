#!/usr/bin/env python3
"""
Automated Build and Packaging Script for Magnetar Client and Addons.
Produces release archives in Builds/Outputs/ matching release specifications.
"""

import os
import re
import sys
import shutil
import zipfile
import urllib.request
import subprocess
from pathlib import Path

# Paths
SCRIPT_DIR = Path(__file__).resolve().parent
ROOT_DIR = SCRIPT_DIR.parent
BUILDS_DIR = ROOT_DIR / "Builds"
OUTPUTS_DIR = BUILDS_DIR / "Outputs"
ASSETS_DIR = BUILDS_DIR / "Assets"

MAGNETAR_PROJ = ROOT_DIR / "Magnetar Client" / "Magnetar Client.csproj"
ASSIST_BOT_PROJ = ROOT_DIR / "Addons" / "Assist Bot" / "Project" / "Assist Bot.csproj"
AUDIO_ADDON_PROJ = ROOT_DIR / "Addons" / "Audio Addon" / "Project" / "AudioAddon.csproj"
SIMPLE_SPAWNER_PROJ = ROOT_DIR.parent / "SimpleSpawner" / "SimpleSpawner.csproj"

MODEL_URL = "https://magnetar-client.onrender.com/aiaddon/api/model/download"
MODEL_CACHE_DIR = BUILDS_DIR / ".cache"
MODEL_CACHE_FILE = MODEL_CACHE_DIR / "buff_model.zip"

SIMPLE_SPAWNER_DEFAULT_DIR = ROOT_DIR.parent / "SimpleSpawner" / "bin"


def log(msg: str):
    print(f"[BUILD] {msg}")


def log_err(msg: str):
    print(f"[ERROR] {msg}", file=sys.stderr)


def get_magnetar_version() -> str:
    info_file = ROOT_DIR / "Magnetar Client" / "src" / "Config" / "MagnetarInfo.cs"
    if not info_file.exists():
        raise FileNotFoundError(f"MagnetarInfo.cs not found at {info_file}")
    content = info_file.read_text(encoding="utf-8")
    m = re.search(r'public\s+const\s+string\s+Version\s*=\s*"([^"]+)";', content)
    if not m:
        raise ValueError("Could not extract Version from MagnetarInfo.cs")
    return m.group(1).strip()


def run_command(cmd, cwd=ROOT_DIR):
    log(f"Running: {' '.join(str(c) for c in cmd)}")
    res = subprocess.run(cmd, cwd=cwd)
    if res.returncode != 0:
        raise RuntimeError(f"Command failed with exit code {res.returncode}: {' '.join(str(c) for c in cmd)}")


def build_all_projects(skip_build: bool = False):
    if skip_build:
        log("Skipping build stage (--skip-build).")
        return

    log("==========================================")
    log("Building Magnetar Client Configurations")
    log("==========================================")
    for cfg in ["Release_Melon", "Release_bepinex", "Android"]:
        log(f"Building Magnetar Client [{cfg}]...")
        run_command(["dotnet", "build", str(MAGNETAR_PROJ), "-c", cfg, "/p:PostBuildEvent=", "--nologo", "-v:q"])

    log("==========================================")
    log("Building Assist Bot Configurations")
    log("==========================================")
    for cfg in ["MelonLoader", "MELON LOCAL", "BepInEx", "BEPINEX LOCAL"]:
        log(f"Building Assist Bot [{cfg}]...")
        run_command(["dotnet", "build", str(ASSIST_BOT_PROJ), "-c", cfg, "--nologo", "-v:q"])

    log("==========================================")
    log("Building Audio Addon Configurations")
    log("==========================================")
    for cfg in ["MelonLoader", "BepInEx"]:
        log(f"Building Audio Addon [{cfg}]...")
        run_command(["dotnet", "build", str(AUDIO_ADDON_PROJ), "-c", cfg, "--nologo", "-v:q"])

    if SIMPLE_SPAWNER_PROJ.exists():
        log("==========================================")
        log("Building Simple Spawner Configurations")
        log("==========================================")
        for cfg in ["Melonloader", "Bepinex"]:
            log(f"Building Simple Spawner [{cfg}]...")
            run_command(["dotnet", "build", str(SIMPLE_SPAWNER_PROJ), "-c", cfg, "--nologo", "-v:q"])


def ensure_buff_model() -> Path:
    """Download buff_model.zip or retrieve from cache / existing staging."""
    MODEL_CACHE_DIR.mkdir(parents=True, exist_ok=True)
    if MODEL_CACHE_FILE.exists() and MODEL_CACHE_FILE.stat().st_size > 0:
        log(f"Using cached buff_model: {MODEL_CACHE_FILE} ({MODEL_CACHE_FILE.stat().st_size} bytes)")
    
    # Try downloading fresh model
    log(f"Attempting download of latest buff_model from {MODEL_URL}...")
    try:
        req = urllib.request.Request(MODEL_URL, headers={"User-Agent": "MagnetarBuildPipeline/1.0"})
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = resp.read()
            if len(data) > 0:
                MODEL_CACHE_FILE.write_bytes(data)
                log(f"Downloaded and cached buff_model.zip ({len(data)} bytes).")
                return MODEL_CACHE_FILE
    except Exception as e:
        log(f"Online download failed ({e}). Checking local fallback sources...")

    if MODEL_CACHE_FILE.exists() and MODEL_CACHE_FILE.stat().st_size > 0:
        return MODEL_CACHE_FILE

    raise FileNotFoundError("buff_model.zip could not be downloaded and no cached model was found in Builds/.cache/.")


def get_simple_spawner_dll(loader: str) -> Path:
    """Return SimpleSpawner dll path for Melonloader or Bepinex."""
    loader_lower = loader.lower()
    if "melon" in loader_lower:
        candidates = [
            SIMPLE_SPAWNER_DEFAULT_DIR / "Melonloader" / "SimpleSpawner.Melonloader.dll",
            SIMPLE_SPAWNER_DEFAULT_DIR / "Melonloader" / "SimpleSpawner.melonloader.dll",
            SIMPLE_SPAWNER_DEFAULT_DIR / "Melonloader" / "SimpleSpawner.dll",
        ]
    else:
        candidates = [
            SIMPLE_SPAWNER_DEFAULT_DIR / "Bepinex" / "SimpleSpawner.Bepinex.dll",
            SIMPLE_SPAWNER_DEFAULT_DIR / "Bepinex" / "SimpleSpawner.bepinex.dll",
            SIMPLE_SPAWNER_DEFAULT_DIR / "Bepinex" / "SimpleSpawner.dll",
        ]

    for c in candidates:
        if c.exists():
            return c
    raise FileNotFoundError(f"SimpleSpawner DLL for {loader} not found. Checked: {[str(c) for c in candidates]}")


def add_magnetar_data(zf: zipfile.ZipFile, prefix: str, target_platform: str):
    """
    Add Magnetar Data files to archive under prefix.
    target_platform: 'pc' (for MelonLoader & BepInEx) or 'android'.
    """
    source_dir = ROOT_DIR / "Magnetar Client" / "Magnetar Data"
    if not source_dir.exists():
        source_dir = ROOT_DIR / "Magnetar Data"
    if not source_dir.exists():
        raise FileNotFoundError(f"Source Magnetar Data not found at {source_dir}")

    for root, dirs, files in os.walk(source_dir):
        # Always omit any Addons folder that might accidentally reside inside Magnetar Data source
        if "Addons" in dirs:
            dirs.remove("Addons")

        rel_root = Path(root).relative_to(source_dir)

        for f in files:
            source_file = Path(root) / f
            rel_file_path = rel_root / f

            # Handle platform-specific asset bundles inside Resources
            if rel_root.parts and rel_root.parts[0].lower() == "resources":
                if target_platform == "pc":
                    if f == "magnetar_ui_android":
                        continue
                    elif f == "magnetar_ui_pc":
                        arc_file = rel_root / "magnetar_ui"
                    else:
                        arc_file = rel_file_path
                else:  # android
                    if f == "magnetar_ui_pc":
                        continue
                    elif f == "magnetar_ui_android":
                        arc_file = rel_root / "magnetar_ui"
                    else:
                        arc_file = rel_file_path
            else:
                arc_file = rel_file_path

            arcname = (Path(prefix) / "Magnetar Data" / arc_file).as_posix()
            zf.write(source_file, arcname)


def get_assist_bot_ml_dlls(bin_dir: Path) -> list[Path]:
    """Get the exact 8 ML and Tensor assemblies required by Assist Bot."""
    required_names = [
        "Microsoft.ML.Core.dll",
        "Microsoft.ML.CpuMath.dll",
        "Microsoft.ML.Data.dll",
        "Microsoft.ML.DataView.dll",
        "Microsoft.ML.dll",
        "Microsoft.ML.StandardTrainers.dll",
        "Microsoft.ML.Transforms.dll",
        "System.Numerics.Tensors.dll",
    ]
    dlls = []
    for name in required_names:
        p = bin_dir / name
        if p.exists():
            dlls.append(p)
        else:
            log_err(f"Warning: Expected Assist Bot dependency '{name}' not found in {bin_dir}")
    return dlls


def package_melonloader(version: str):
    log("Packaging MelonLoader archives...")
    bin_melon = ROOT_DIR / "Magnetar Client" / "bin" / "Release_Melon"
    install_txt = ASSETS_DIR / "Melonloader" / "How to install.txt"
    spawner_dll = get_simple_spawner_dll("Melonloader")
    assist_bot_bin = ROOT_DIR / "Addons" / "Assist Bot" / "Project" / "bin" / "MelonLoader"

    client_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Melonloader.zip"
    full_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Melonloader-FULL.zip"

    # Base MelonLoader
    with zipfile.ZipFile(client_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        if install_txt.exists():
            zf.write(install_txt, "How to install.txt")
        zf.write(bin_melon / "Magnetar Client.dll", "Mods/Magnetar Client.dll")
        add_magnetar_data(zf, prefix="Mods", target_platform="pc")
        zf.write(bin_melon / "DiscordRPC.dll", "UserLibs/DiscordRPC.dll")
        if (bin_melon / "Newtonsoft.Json.dll").exists():
            zf.write(bin_melon / "Newtonsoft.Json.dll", "UserLibs/Newtonsoft.Json.dll")

    log(f"Created: {client_zip.name}")

    # FULL MelonLoader
    with zipfile.ZipFile(full_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        if install_txt.exists():
            zf.write(install_txt, "How to install.txt")
        zf.write(bin_melon / "Magnetar Client.dll", "Mods/Magnetar Client.dll")
        zf.write(spawner_dll, f"Mods/{spawner_dll.name}")
        add_magnetar_data(zf, prefix="Mods", target_platform="pc")
        # Addons
        zf.write(assist_bot_bin / "Assist Bot.dll", "Mods/Magnetar Data/Addons/Assist Bot.dll")

        audio_melon_bin = ROOT_DIR / "Addons" / "Audio Addon" / "Project" / "bin" / "MelonLoader"
        zf.write(audio_melon_bin / "AudioAddon.dll", "Mods/Magnetar Data/Addons/AudioAddon.dll")
        zf.write(audio_melon_bin / "AudioImportLib.dll", "Mods/AudioImportLib.dll")

        zf.write(bin_melon / "DiscordRPC.dll", "UserLibs/DiscordRPC.dll")
        if (bin_melon / "Newtonsoft.Json.dll").exists():
            zf.write(bin_melon / "Newtonsoft.Json.dll", "UserLibs/Newtonsoft.Json.dll")

        for ml_dll in get_assist_bot_ml_dlls(assist_bot_bin):
            zf.write(ml_dll, f"UserLibs/{ml_dll.name}")

    log(f"Created: {full_zip.name}")


def package_bepinex(version: str):
    log("Packaging BepInEx archives...")
    bin_bep = ROOT_DIR / "Magnetar Client" / "bin" / "Release_bepinex"
    install_txt = ASSETS_DIR / "Bepinex" / "How to install.txt"
    spawner_dll = get_simple_spawner_dll("Bepinex")
    assist_bot_bin = ROOT_DIR / "Addons" / "Assist Bot" / "Project" / "bin" / "BepInEx"

    client_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Bepinex.zip"
    full_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Bepinex-FULL.zip"

    # Base BepInEx
    with zipfile.ZipFile(client_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        if install_txt.exists():
            zf.write(install_txt, "How to install.txt")
        zf.write(bin_bep / "Magnetar Client.dll", "BepInEx/plugins/Magnetar Client.dll")
        zf.write(bin_bep / "DiscordRPC.dll", "BepInEx/plugins/DiscordRPC.dll")
        add_magnetar_data(zf, prefix="BepInEx/plugins", target_platform="pc")
        if (bin_bep / "Newtonsoft.Json.dll").exists():
            zf.write(bin_bep / "Newtonsoft.Json.dll", "BepInEx/core/Newtonsoft.Json.dll")

    log(f"Created: {client_zip.name}")

    # FULL BepInEx
    with zipfile.ZipFile(full_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        if install_txt.exists():
            zf.write(install_txt, "How to install.txt")
        zf.write(bin_bep / "Magnetar Client.dll", "BepInEx/plugins/Magnetar Client.dll")
        zf.write(bin_bep / "DiscordRPC.dll", "BepInEx/plugins/DiscordRPC.dll")
        zf.write(spawner_dll, f"BepInEx/plugins/{spawner_dll.name}")
        add_magnetar_data(zf, prefix="BepInEx/plugins", target_platform="pc")
        # Addons
        zf.write(assist_bot_bin / "Assist Bot.dll", "BepInEx/plugins/Magnetar Data/Addons/Assist Bot.dll")

        audio_bep_bin = ROOT_DIR / "Addons" / "Audio Addon" / "Project" / "bin" / "BepInEx"
        zf.write(audio_bep_bin / "AudioAddon.dll", "BepInEx/plugins/Magnetar Data/Addons/AudioAddon.dll")
        zf.write(audio_bep_bin / "SherkeyXD.BepInEx-Sounds-Loader.dll", "Mods/SherkeyXD.BepInEx-Sounds-Loader.dll")

        if (bin_bep / "Newtonsoft.Json.dll").exists():
            zf.write(bin_bep / "Newtonsoft.Json.dll", "BepInEx/core/Newtonsoft.Json.dll")

        for ml_dll in get_assist_bot_ml_dlls(assist_bot_bin):
            zf.write(ml_dll, f"BepInEx/core/{ml_dll.name}")

    log(f"Created: {full_zip.name}")


def package_android(version: str):
    log("Packaging Android archives...")
    bin_android = ROOT_DIR / "Magnetar Client" / "bin" / "Android"
    icon_png = ASSETS_DIR / "Android" / "icon.png"
    modpack_json = ASSETS_DIR / "Android" / "modpack.json"
    assist_bot_bin = ROOT_DIR / "Addons" / "Assist Bot" / "Project" / "bin" / "BepInEx"

    client_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Android.zip"
    full_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Android-FULL.zip"

    # Base Android
    with zipfile.ZipFile(client_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        if icon_png.exists():
            zf.write(icon_png, "icon.png")
        if modpack_json.exists():
            zf.write(modpack_json, "modpack.json")
        zf.write(bin_android / "Magnetar Client.dll", "plugins/Magnetar Client.dll")
        add_magnetar_data(zf, prefix="plugins", target_platform="android")
        if (bin_android / "Newtonsoft.Json.dll").exists():
            zf.write(bin_android / "Newtonsoft.Json.dll", "core/Newtonsoft.Json.dll")

    log(f"Created: {client_zip.name}")

    # FULL Android (Client + AssistBot Online, NO SimpleSpawner)
    with zipfile.ZipFile(full_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        if icon_png.exists():
            zf.write(icon_png, "icon.png")
        if modpack_json.exists():
            zf.write(modpack_json, "modpack.json")
        zf.write(bin_android / "Magnetar Client.dll", "plugins/Magnetar Client.dll")
        add_magnetar_data(zf, prefix="plugins", target_platform="android")
        # Addons
        zf.write(assist_bot_bin / "Assist Bot.dll", "plugins/Magnetar Data/Addons/Assist Bot.dll")

        audio_bep_bin = ROOT_DIR / "Addons" / "Audio Addon" / "Project" / "bin" / "BepInEx"
        zf.write(audio_bep_bin / "AudioAddon.dll", "plugins/Magnetar Data/Addons/AudioAddon.dll")
        zf.write(audio_bep_bin / "SherkeyXD.BepInEx-Sounds-Loader.dll", "Mods/SherkeyXD.BepInEx-Sounds-Loader.dll")

        if (bin_android / "Newtonsoft.Json.dll").exists():
            zf.write(bin_android / "Newtonsoft.Json.dll", "core/Newtonsoft.Json.dll")

        for ml_dll in get_assist_bot_ml_dlls(assist_bot_bin):
            zf.write(ml_dll, f"core/{ml_dll.name}")

    log(f"Created: {full_zip.name}")


def package_addons(version: str, model_path: Path):
    log("Packaging Addons archive...")
    assist_bot_root = ROOT_DIR / "Addons" / "Assist Bot" / "Project" / "bin"
    audio_addon_root = ROOT_DIR / "Addons" / "Audio Addon" / "Project" / "bin"

    # Staging assist bot 4 archives into temporary files
    assist_zips = {}

    # 1. BepInEx Offline
    bin_bep_local = assist_bot_root / "BEPINEX LOCAL"
    bep_offline_name = f"MagnetarClient.AssistBot-{version}-BepInEx-Offline.zip"
    bep_offline_tmp = OUTPUTS_DIR / bep_offline_name
    with zipfile.ZipFile(bep_offline_tmp, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(bin_bep_local / "Assist Bot.dll", "BepInEx/plugins/Magnetar Data/Addons/Assist Bot.dll")
        zf.write(model_path, "BepInEx/plugins/Magnetar Data/Addons/ML/buff_model.zip")
        for ml_dll in get_assist_bot_ml_dlls(bin_bep_local):
            zf.write(ml_dll, f"BepInEx/core/{ml_dll.name}")
    assist_zips[bep_offline_name] = bep_offline_tmp

    # 2. BepInEx Online
    bin_bep = assist_bot_root / "BepInEx"
    bep_online_name = f"MagnetarClient.AssistBot-{version}-BepInEx-Online.zip"
    bep_online_tmp = OUTPUTS_DIR / bep_online_name
    with zipfile.ZipFile(bep_online_tmp, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(bin_bep / "Assist Bot.dll", "BepInEx/plugins/Magnetar Data/Addons/Assist Bot.dll")
        for ml_dll in get_assist_bot_ml_dlls(bin_bep):
            zf.write(ml_dll, f"BepInEx/core/{ml_dll.name}")
    assist_zips[bep_online_name] = bep_online_tmp

    # 3. MelonLoader Offline
    bin_melon_local = assist_bot_root / "MELON LOCAL"
    melon_offline_name = f"MagnetarClient.AssistBot-{version}-Melonloader-Offline.zip"
    melon_offline_tmp = OUTPUTS_DIR / melon_offline_name
    with zipfile.ZipFile(melon_offline_tmp, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(bin_melon_local / "Assist Bot.dll", "Mods/Magnetar Data/Addons/Assist Bot.dll")
        zf.write(model_path, "Mods/Magnetar Data/Addons/ML/buff_model.zip")
        for ml_dll in get_assist_bot_ml_dlls(bin_melon_local):
            zf.write(ml_dll, f"UserLibs/{ml_dll.name}")
    assist_zips[melon_offline_name] = melon_offline_tmp

    # 4. MelonLoader Online
    bin_melon = assist_bot_root / "MelonLoader"
    melon_online_name = f"MagnetarClient.AssistBot-{version}-Melonloader-Online.zip"
    melon_online_tmp = OUTPUTS_DIR / melon_online_name
    with zipfile.ZipFile(melon_online_tmp, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.write(bin_melon / "Assist Bot.dll", "Mods/Magnetar Data/Addons/Assist Bot.dll")
        for ml_dll in get_assist_bot_ml_dlls(bin_melon):
            zf.write(ml_dll, f"UserLibs/{ml_dll.name}")
    assist_zips[melon_online_name] = melon_online_tmp

    # Master Addons Zip: Option B (Assist Bot folder with 4 zips, Audio Addon folder with subfolders)
    addons_zip = OUTPUTS_DIR / f"MagnetarClient-{version}-Addons.zip"
    with zipfile.ZipFile(addons_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        # Assist Bot folder with the 4 zips
        for arc_name, file_path in assist_zips.items():
            zf.write(file_path, f"Assist Bot/{arc_name}")

        # Audio Addon MelonLoader
        audio_melon_bin = audio_addon_root / "MelonLoader"
        zf.write(audio_melon_bin / "AudioAddon.dll", "Audio Addon/Melonloader/Addons/AudioAddon.dll")
        zf.write(audio_melon_bin / "AudioImportLib.dll", "Audio Addon/Melonloader/Mods/AudioImportLib.dll")

        # Audio Addon BepInEx
        audio_bep_bin = audio_addon_root / "BepInEx"
        zf.write(audio_bep_bin / "AudioAddon.dll", "Audio Addon/BepInEx/Addons/AudioAddon.dll")
        zf.write(audio_bep_bin / "SherkeyXD.BepInEx-Sounds-Loader.dll", "Audio Addon/BepInEx/Mods/SherkeyXD.BepInEx-Sounds-Loader.dll")

    log(f"Created: {addons_zip.name}")

    # Clean up the temporary standalone Assist Bot zips from Outputs dir
    for tmp_zip in assist_zips.values():
        if tmp_zip.exists():
            tmp_zip.unlink()


def copy_client_xml():
    log("Copying Magnetar.Client.xml...")
    # Look for Magnetar Client.xml in build output
    candidates = [
        ROOT_DIR / "Magnetar Client" / "bin" / "Release_Melon" / "Magnetar Client.xml",
        ROOT_DIR / "Magnetar Client" / "bin" / "Release_bepinex" / "Magnetar Client.xml",
        ROOT_DIR / "Magnetar Client" / "bin" / "Android" / "Magnetar Client.xml",
        ROOT_DIR / "References" / "Magnetar Client.xml",
    ]
    src_xml = next((c for c in candidates if c.exists()), None)
    if not src_xml:
        raise FileNotFoundError("Could not find source Magnetar Client.xml in any build output.")

    dest_xml = OUTPUTS_DIR / "Magnetar.Client.xml"
    shutil.copy2(src_xml, dest_xml)
    log(f"Copied {src_xml.name} -> {dest_xml}")


def main():
    skip_build = "--skip-build" in sys.argv
    version = get_magnetar_version()
    log(f"=====================================================")
    log(f" Magnetar Client & Addons Build and Release Pipeline")
    log(f" Target Version: {version}")
    log(f" Output Directory: {OUTPUTS_DIR}")
    log(f"=====================================================")

    OUTPUTS_DIR.mkdir(parents=True, exist_ok=True)

    # 1. Build projects
    build_all_projects(skip_build=skip_build)

    # 2. Get buff_model
    model_path = ensure_buff_model()

    # 3. Create packages
    package_melonloader(version)
    package_bepinex(version)
    package_android(version)
    package_addons(version, model_path)
    copy_client_xml()

    log("=====================================================")
    log(" Build and Packaging Completed Successfully!")
    log(" Outputs generated:")
    for item in sorted(OUTPUTS_DIR.iterdir()):
        size_mb = item.stat().st_size / (1024 * 1024)
        log(f"  * {item.name:42} ({size_mb:.2f} MB)")
    log("=====================================================")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        log_err(f"Pipeline failed: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
