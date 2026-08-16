import os
import shutil
import sys
from pathlib import Path


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

    print(f"Scanning '{output_path}' for DLL files to update...")
    updated_count = 0

    # Iterate through all files in the output directory
    for out_file in output_path.rglob("*.dll"):
        # Look for a file with the exact same relative name/structure in the input directory
        relative_path = out_file.relative_to(output_path)
        corresponding_in_file = input_path / relative_path

        if corresponding_in_file.is_file():
            try:
                # Copy the new DLL over the old one, preserving metadata
                shutil.copy2(corresponding_in_file, out_file)
                print(f"Updated: {relative_path}")
                updated_count += 1
            except Exception as e:
                print(f"Failed to update {relative_path}. Error: {e}")
        else:
            print(f"Skipped: {relative_path} (No matching file in input path)")

    print(f"\nUpdate complete. Total DLLs updated: {updated_count}")


if __name__ == "__main__":
    # Example usage via command line arguments
    if len(sys.argv) < 3:
        print("Usage: python update dll.py <input_folder_path> <output_folder_path>")
        sys.exit(1)

    in_dir = sys.argv[1]
    out_dir = sys.argv[2]

    update_dll_files(in_dir, out_dir)
