import os
import shutil
import sys
from pathlib import Path


def sync_dll_directory(input_path: Path, target_path: Path) -> int:
    """Scans target_path for *.dll files and updates them if a matching file exists in input_path."""
    if not target_path.is_dir():
        return 0

    print(f"Scanning '{target_path}' for DLL files to update...")
    updated_count = 0

    for out_file in target_path.rglob("*.dll"):
        relative_path = out_file.relative_to(target_path)
        corresponding_in_file = input_path / relative_path

        if corresponding_in_file.is_file():
            try:
                shutil.copy2(corresponding_in_file, out_file)
                print(f"Updated: {relative_path}")
                updated_count += 1
            except Exception as e:
                print(f"Failed to update {relative_path}. Error: {e}")
        else:
            print(f"Skipped: {relative_path} (No matching file in input path)")

    return updated_count


def get_workspace_root() -> Path:
    """Finds the workspace root directory (where .git or Magnetar Client.slnx or Addons lives)."""
    script_dir = Path(__file__).resolve().parent
    for parent in [script_dir] + list(script_dir.parents):
        if (parent / "Magnetar Client.slnx").is_file() or (parent / "Addons").is_dir():
            return parent
    return script_dir.parent.parent


def get_main_reference_dir() -> Path:
    """Returns the primary References directory inside Magnetar Client."""
    root = get_workspace_root()
    return root / "Magnetar Client" / "References"


def get_addon_reference_dirs() -> list[Path]:
    """Discovers all Addons/*/Project/References directories across the workspace."""
    root = get_workspace_root()
    addons_root = root / "Addons"
    found = []
    seen = set()
    if addons_root.is_dir():
        for ref_dir in addons_root.glob("*/Project/References"):
            if ref_dir.is_dir():
                resolved = ref_dir.resolve()
                if resolved not in seen:
                    found.append(ref_dir)
                    seen.add(resolved)
    return found


def update_dll_files(input_dir_path, output_dir_path):
    input_path = Path(input_dir_path)
    output_path = Path(output_dir_path)

    # Validate that both paths exist and are directories
    if not input_path.is_dir():
        print(f"Error: Input path '{input_dir_path}' is not a valid directory.")
        return
    if not output_path.is_dir():
        print(f"Error: Output path '{output_dir_path}' is not a valid directory.")
        return

    # 1. Update the primary target directory (e.g. Magnetar Client/References/...)
    total_updated = sync_dll_directory(input_path, output_path)

    # 2. Determine relative subpath within Magnetar Client/References (if applicable)
    main_ref_dir = get_main_reference_dir().resolve()
    try:
        rel_subpath = output_path.resolve().relative_to(main_ref_dir)
    except ValueError:
        rel_subpath = None

    # 3. Update matching subdirectories in all Addons/*/Project/References
    addon_ref_dirs = get_addon_reference_dirs()
    if addon_ref_dirs:
        for addon_ref in addon_ref_dirs:
            addon_target = (addon_ref / rel_subpath) if rel_subpath else addon_ref
            if addon_target.is_dir():
                print(f"\n[Addon Sync] Updating: {addon_target}")
                addon_updated = sync_dll_directory(input_path, addon_target)
                total_updated += addon_updated

    print(f"\nUpdate complete across all targets. Total DLLs updated: {total_updated}")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python update_dll.py <input_folder_path> <output_folder_path>")
        sys.exit(1)

    in_dir = sys.argv[1]
    out_dir = sys.argv[2]

    update_dll_files(in_dir, out_dir)
