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


def update_dll_files(input_dir_path, output_dir_path):
    input_path = Path(input_dir_path).resolve()
    output_path = Path(output_dir_path).resolve()

    # Validate that both paths exist and are directories
    if not input_path.is_dir():
        print(f"Error: Input path '{input_dir_path}' is not a valid directory.")
        return
    if not output_path.is_dir():
        print(f"Error: Output path '{output_dir_path}' is not a valid directory.")
        return

    # Update the primary base target directory (e.g. References/...)
    total_updated = sync_dll_directory(input_path, output_path)

    print(f"\nUpdate complete. Total DLLs updated: {total_updated}")


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python update_dll.py <input_folder_path> <output_folder_path>")
        sys.exit(1)

    in_dir = sys.argv[1]
    out_dir = sys.argv[2]

    update_dll_files(in_dir, out_dir)
