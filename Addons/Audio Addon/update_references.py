#!/usr/bin/env python3
"""
Magnetar Client Reference DLL Updater
Downloads or synchronizes reference DLLs for Magnetar Client addons directly from
the Addon Template folder on GitHub (Tproplay/Magnetar-Client) or from the local workspace.
"""

import argparse
import os
import shutil
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

GITHUB_REPO = "Tproplay/Magnetar-Client"
DEFAULT_BRANCH = "devlop"

# Primary source: Addons/Addon Template/Project/References
PRIMARY_RAW_URL = f"https://raw.githubusercontent.com/{GITHUB_REPO}/{{branch}}/Addons/Addon%20Template/Project/References"
# Fallback source: Magnetar Client/References
FALLBACK_RAW_URL = f"https://raw.githubusercontent.com/{GITHUB_REPO}/{{branch}}/Magnetar%20Client/References"

# Complete manifest of reference files
REFERENCE_FILES = [
    # Core Magnetar Client Assemblies
    "Magnetar Client.dll",
    "Magnetar Client.xml",
    # BepInEx
    "Bepinex/SherkeyXD.BepInEx-Sounds-Loader.dll",
    "Bepinex/0Harmony.dll",
    "Bepinex/BepInEx.Core.dll",
    "Bepinex/BepInEx.Unity.IL2CPP.dll",
    "Bepinex/Il2CppInterop.Common.dll",
    "Bepinex/Il2CppInterop.Runtime.dll",
    "Bepinex/Il2Cpp/Assembly-CSharp.dll",
    "Bepinex/Il2Cpp/Il2Cppmscorlib.dll",
    "Bepinex/Il2Cpp/UniTask.dll",
    "Bepinex/Il2Cpp/Unity.TextMeshPro.dll",
    "Bepinex/Il2Cpp/UnityEngine.AnimationModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.AssetBundleModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.CoreModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.IMGUIModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.ImageConversionModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.InputLegacyModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.ParticleSystemModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.Physics2DModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.TextRenderingModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.UI.dll",
    "Bepinex/Il2Cpp/UnityEngine.UIModule.dll",
    "Bepinex/Il2Cpp/UnityEngine.dll",
    # MelonLoader
    "Melonloader/AudioImportLib.dll",
    "Melonloader/0Harmony.dll",
    "Melonloader/Il2CppInterop.Common.dll",
    "Melonloader/Il2CppInterop.Runtime.dll",
    "Melonloader/MelonLoader.dll",
    "Melonloader/Newtonsoft.Json.dll",
    "Melonloader/Il2Cpp/Assembly-CSharp.dll",
    "Melonloader/Il2Cpp/Il2CppUniTask.dll",
    "Melonloader/Il2Cpp/Il2Cppmscorlib.dll",
    "Melonloader/Il2Cpp/Unity.TextMeshPro.dll",
    "Melonloader/Il2Cpp/UnityEngine.AnimationModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.AssetBundleModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.ClothModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.CoreModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.IMGUIModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.ImageConversionModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.InputLegacyModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.ParticleSystemModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.Physics2DModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.TextRenderingModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.UI.dll",
    "Melonloader/Il2Cpp/UnityEngine.UIModule.dll",
    "Melonloader/Il2Cpp/UnityEngine.dll",
]


def find_workspace_root() -> Path | None:
    current = Path(__file__).resolve().parent
    for parent in [current] + list(current.parents):
        if (parent / "Magnetar Client.slnx").is_file() or (parent / "Addons" / "Addon Template").is_dir():
            return parent
    return None


def find_target_references_dir() -> Path:
    script_dir = Path(__file__).resolve().parent
    if (script_dir / "References").is_dir() or script_dir.name == "Project":
        return script_dir / "References"
    if (script_dir / "Project" / "References").is_dir():
        return script_dir / "Project" / "References"
    if (script_dir / "Project").is_dir():
        return script_dir / "Project" / "References"
    return script_dir / "References"


def get_local_source_dir() -> Path | None:
    """Finds the local reference source directory, prioritizing Addon Template/Project/References."""
    root = find_workspace_root()
    if not root:
        return None

    addon_template_refs = root / "Addons" / "Addon Template" / "Project" / "References"
    if addon_template_refs.is_dir():
        return addon_template_refs

    fallback_refs = root / "Magnetar Client" / "References"
    if fallback_refs.is_dir():
        return fallback_refs

    return None


def copy_from_local(local_source: Path, target_dir: Path) -> int:
    """Syncs reference files from local Addon Template/Project/References (or fallback)."""
    copied = 0
    print(f"[*] Copying references from local source: {local_source}")
    for rel_path in REFERENCE_FILES:
        src = local_source / rel_path
        dst = target_dir / rel_path
        if src.is_file():
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
            copied += 1
            print(f"  [OK] {rel_path}")
        else:
            # Fallback search if file wasn't found in primary local_source
            root = find_workspace_root()
            if root:
                alt = (root / "Magnetar Client" / "References" / rel_path) if "Magnetar Client" not in rel_path else None
                if alt and alt.is_file():
                    dst.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(alt, dst)
                    copied += 1
                    print(f"  [OK] {rel_path} (from Magnetar Client/References)")
                    continue
            print(f"  [!] Missing locally: {rel_path}")

    return copied


def download_from_github(target_dir: Path, branch: str = DEFAULT_BRANCH) -> int:
    """Downloads reference files directly from GitHub's Addon Template folder."""
    primary_url = PRIMARY_RAW_URL.format(branch=branch)
    fallback_url = FALLBACK_RAW_URL.format(branch=branch)
    print(f"[*] Downloading references from GitHub: {GITHUB_REPO} (branch: {branch})...")
    print(f"    Source: Addons/Addon Template/Project/References\n")
    downloaded = 0
    headers = {"User-Agent": "Magnetar-Reference-Updater/1.0"}

    for idx, rel_path in enumerate(REFERENCE_FILES, 1):
        encoded_path = urllib.parse.quote(rel_path)
        url = f"{primary_url}/{encoded_path}"
        dst = target_dir / rel_path
        dst.parent.mkdir(parents=True, exist_ok=True)

        print(f"  [{idx}/{len(REFERENCE_FILES)}] Fetching {rel_path}...", end=" ", flush=True)

        # Try primary URL (Addon Template/Project/References)
        success = False
        try:
            req = urllib.request.Request(url, headers=headers)
            with urllib.request.urlopen(req, timeout=30) as resp, open(dst, "wb") as out_file:
                shutil.copyfileobj(resp, out_file)
            print("OK")
            downloaded += 1
            success = True
        except urllib.error.HTTPError as e:
            if e.code == 404:
                # Try fallback URL (Magnetar Client/References)
                try:
                    fb_url = f"{fallback_url}/{encoded_path}"
                    req = urllib.request.Request(fb_url, headers=headers)
                    with urllib.request.urlopen(req, timeout=30) as resp, open(dst, "wb") as out_file:
                        shutil.copyfileobj(resp, out_file)
                    print("OK (via fallback)")
                    downloaded += 1
                    success = True
                except Exception:
                    pass
            if not success:
                print(f"FAILED ({e})")
        except Exception as e:
            print(f"FAILED ({e})")

    # If Magnetar Client.dll failed to download from GitHub, try local copy if available
    target_client_dll = target_dir / "Magnetar Client.dll"
    if not target_client_dll.is_file():
        root = find_workspace_root()
        if root:
            candidates = [
                root / "Addons" / "Addon Template" / "Project" / "References" / "Magnetar Client.dll",
                root / "Addons" / "Assist Bot" / "Project" / "References" / "Magnetar Client.dll",
                root / "Magnetar Client" / "bin" / "Bepinex" / "Bepinex" / "Magnetar Client.dll",
            ]
            for cand in candidates:
                if cand.is_file():
                    shutil.copy2(cand, target_client_dll)
                    print("  [OK] Magnetar Client.dll copied from local project.")
                    downloaded += 1
                    break

    return downloaded


def update_references(target_dir: Path, force_github: bool = False, branch: str = DEFAULT_BRANCH) -> int:
    target_dir.mkdir(parents=True, exist_ok=True)
    local_source = get_local_source_dir()

    if local_source and local_source.is_dir() and not force_github:
        return copy_from_local(local_source, target_dir)
    else:
        return download_from_github(target_dir, branch=branch)


def main():
    parser = argparse.ArgumentParser(description="Update or download Magnetar Client reference DLLs from Addon Template.")
    parser.add_argument("--target", "-t", type=str, default=None, help="Target References directory (default: auto-detected)")
    parser.add_argument("--github", "-g", action="store_true", help="Force download from GitHub Addon Template folder even if local files exist")
    parser.add_argument("--branch", "-b", type=str, default=DEFAULT_BRANCH, help=f"GitHub branch to download from (default: {DEFAULT_BRANCH})")
    args = parser.parse_args()

    target = Path(args.target).resolve() if args.target else find_target_references_dir()
    print(f"=== Magnetar Client Reference Updater ===")
    print(f"Target Directory: {target}\n")

    count = update_references(target, force_github=args.github, branch=args.branch)
    print(f"\nDone! Successfully updated {count} reference file(s).")


if __name__ == "__main__":
    main()
