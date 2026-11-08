#!/usr/bin/env python3
"""
Import and synchronize translation files from the test game directory to Magnetar Data.
Target Language: English

Actions:
  1. Replace 'Modules/' in Dir 1 with 'Modules/' from Dir 2.
  2. Override 'hud_translations.json' from Dir 2 to Dir 1.
  3. Override 'translation_strings.json' from Dir 2 to Dir 1,
     excluding entries whose keys match any pattern in 'translation_regexs.json'.
  4. Sync 'Enums/': copy brand new enum files, and for existing files,
     only add new keys from Dir 2 without modifying existing Dir 1 values.
"""

import argparse
import json
import os
import re
import shutil
import sys
from pathlib import Path

# Paths
SCRIPT_DIR = Path(__file__).resolve().parent
ROOT_DIR = SCRIPT_DIR.parent.parent
DEFAULT_TEST_DIR = Path(r"C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1")

DIR1_ENGLISH = ROOT_DIR / "Magnetar Client" / "Magnetar Data" / "Magnetar Translation" / "English"


def log(msg: str):
    print(f"[TRANSLATION] {msg}")


def log_err(msg: str):
    print(f"[ERROR] {msg}", file=sys.stderr)


def load_json(path: Path) -> dict:
    if not path.is_file():
        return {}
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def save_json(path: Path, data: dict, indent: int = 2):
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=indent)
        f.write("\n")


def sync_modules(dir1_english: Path, dir2_english: Path):
    log("--- Step 1: Syncing Modules folder ---")
    dir1_modules = dir1_english / "Modules"
    dir2_modules = dir2_english / "Modules"

    if not dir2_modules.is_dir():
        log_err(f"Source Modules directory not found: {dir2_modules}")
        return

    # Count source files
    src_files = list(dir2_modules.glob("*.json"))
    if not src_files:
        log_err(f"Source Modules directory is empty, skipping replace for safety: {dir2_modules}")
        return

    if dir1_modules.exists():
        log(f"Removing old Modules folder: {dir1_modules}")
        shutil.rmtree(dir1_modules)

    log(f"Copying {len(src_files)} module files from {dir2_modules} -> {dir1_modules}")
    shutil.copytree(dir2_modules, dir1_modules)
    log("Modules synced successfully.")


def sync_hud_translations(dir1_english: Path, dir2_english: Path):
    log("--- Step 2: Overriding hud_translations.json ---")
    dir1_hud = dir1_english / "hud_translations.json"
    dir2_hud = dir2_english / "hud_translations.json"

    if not dir2_hud.is_file():
        log_err(f"Source hud_translations.json not found: {dir2_hud}")
        return

    shutil.copy2(dir2_hud, dir1_hud)
    log(f"Overwrote {dir1_hud.name} ({dir2_hud.stat().st_size} bytes).")


def sync_translation_strings(dir1_english: Path, dir2_english: Path):
    log("--- Step 3: Syncing translation_strings.json (filtered by regex) ---")
    dir1_regex_file = dir1_english / "translation_regexs.json"
    dir1_strings_file = dir1_english / "translation_strings.json"
    dir2_strings_file = dir2_english / "translation_strings.json"

    if not dir2_strings_file.is_file():
        log_err(f"Source translation_strings.json not found: {dir2_strings_file}")
        return

    # 1. Load regex patterns from Dir 1
    regex_data = load_json(dir1_regex_file)
    compiled_regexes = []
    for pattern in regex_data.keys():
        try:
            compiled_regexes.append(re.compile(pattern))
        except re.error as e:
            log_err(f"Invalid regex '{pattern}': {e}")
    log(f"Loaded {len(compiled_regexes)} regex filter patterns from {dir1_regex_file.name}.")

    # 2. Load strings
    dir1_strings = load_json(dir1_strings_file)
    dir2_strings = load_json(dir2_strings_file)

    log(f"Dir 1 strings before: {len(dir1_strings)}")
    log(f"Dir 2 strings count:  {len(dir2_strings)}")

    overridden_count = 0
    added_count = 0
    skipped_regex_count = 0

    for key, val in dir2_strings.items():
        # Check if key matches any regex pattern
        if any(rgx.search(key) for rgx in compiled_regexes):
            skipped_regex_count += 1
            continue

        if key in dir1_strings:
            if dir1_strings[key] != val:
                dir1_strings[key] = val
                overridden_count += 1
        else:
            dir1_strings[key] = val
            added_count += 1

    save_json(dir1_strings_file, dir1_strings, indent=2)
    log(f"Updated translation_strings.json: +{added_count} new, {overridden_count} updated, {skipped_regex_count} filtered out by regex. Total now: {len(dir1_strings)}.")


def sort_enum_dict(data: dict) -> dict:
    """Sort dictionary keys numerically if all keys are digits, else lexicographically."""
    try:
        if all(k.isdigit() or (k.startswith("-") and k[1:].isdigit()) for k in data.keys()):
            return dict(sorted(data.items(), key=lambda item: int(item[0])))
    except Exception:
        pass
    return dict(sorted(data.items(), key=lambda item: item[0]))


def sync_enums(dir1_english: Path, dir2_english: Path):
    log("--- Step 4: Syncing Enums (only new values and new files) ---")
    dir1_enums = dir1_english / "Enums"
    dir2_enums = dir2_english / "Enums"

    if not dir2_enums.is_dir():
        log_err(f"Source Enums directory not found: {dir2_enums}")
        return

    dir1_enums.mkdir(parents=True, exist_ok=True)
    dir2_files = list(dir2_enums.glob("*.json"))

    new_files_count = 0
    updated_files_count = 0
    total_new_keys = 0

    for src_file in dir2_files:
        dest_file = dir1_enums / src_file.name

        # Case A: Brand new enum file
        if not dest_file.exists():
            shutil.copy2(src_file, dest_file)
            log(f"  [+] New Enum File: {src_file.name}")
            new_files_count += 1
            continue

        # Case B: Enum file exists in both -> Only add new keys
        dest_data = load_json(dest_file)
        src_data = load_json(src_file)

        added_keys_in_file = 0
        for k, v in src_data.items():
            if k not in dest_data:
                dest_data[k] = v
                added_keys_in_file += 1
                total_new_keys += 1

        if added_keys_in_file > 0:
            dest_data = sort_enum_dict(dest_data)
            save_json(dest_file, dest_data, indent=2)
            log(f"  [*] {src_file.name}: Added {added_keys_in_file} new enum key(s)")
            updated_files_count += 1

    log(f"Enums sync complete: {new_files_count} new file(s), {updated_files_count} file(s) updated with {total_new_keys} new key(s).")


def main():
    parser = argparse.ArgumentParser(description="Auto-sync English translation files from testing game dir to project.")
    parser.add_argument(
        "--test-dir",
        "-t",
        type=Path,
        default=DEFAULT_TEST_DIR,
        help=f"Path to testing game directory (default: {DEFAULT_TEST_DIR})",
    )
    args = parser.parse_args()

    test_game_dir = args.test_dir
    dir2_english = test_game_dir / "Mods" / "Magnetar Data" / "Magnetar Translation" / "English"

    log("=" * 60)
    log("  Magnetar Translation Auto-Sync Pipeline (English)")
    log("=" * 60)
    log(f"Project Translation Dir (Dir 1): {DIR1_ENGLISH}")
    log(f"Testing Game Dir        (Dir 2): {dir2_english}")

    if not dir2_english.exists():
        log_err(f"Testing translation path does not exist:\n  {dir2_english}")
        log_err("Please verify your game path or pass --test-dir <path>.")
        sys.exit(1)

    if not DIR1_ENGLISH.exists():
        log_err(f"Project translation path does not exist:\n  {DIR1_ENGLISH}")
        sys.exit(1)

    # Execute the 4 steps
    sync_modules(DIR1_ENGLISH, dir2_english)
    sync_hud_translations(DIR1_ENGLISH, dir2_english)
    sync_translation_strings(DIR1_ENGLISH, dir2_english)
    sync_enums(DIR1_ENGLISH, dir2_english)

    log("=" * 60)
    log("  Translation Auto-Sync Completed Successfully!")
    log("=" * 60)


if __name__ == "__main__":
    main()
