import os
import json
import argparse
import shutil

def merge_dicts(main_data, temp_data):
    for key, val in temp_data.items():
        if key in main_data:
            if isinstance(val, dict) and isinstance(main_data[key], dict):
                merge_dicts(main_data[key], val)
            else:
                main_data[key] = val
        else:
            main_data[key] = val

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("lang_folder")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    lang_dir = os.path.join(script_dir, args.lang_folder)
    base_temp_dir = os.path.join(script_dir, "temp_untranslated")
    temp_dir = os.path.join(base_temp_dir, args.lang_folder)

    if not os.path.exists(temp_dir):
        print(f"Error: Temporary folder '{temp_dir}' does not exist.")
        return
    if not os.path.exists(lang_dir):
        print(f"Error: Target language folder '{lang_dir}' does not exist.")
        return

    print(f"--- Applying translations to '{args.lang_folder}' ---")

    for root, dirs, files in os.walk(temp_dir):
        for file in files:
            if file.endswith('.json'):
                rel_path = os.path.relpath(root, temp_dir)
                lang_file_dir = os.path.normpath(os.path.join(lang_dir, rel_path))
                lang_file_path = os.path.join(lang_file_dir, file)
                temp_file_path = os.path.join(root, file)

                if not os.path.exists(lang_file_path):
                    continue

                with open(lang_file_path, 'r', encoding='utf-8') as f:
                    try:
                        lang_data = json.load(f)
                    except json.JSONDecodeError:
                        lang_data = {}

                with open(temp_file_path, 'r', encoding='utf-8') as f:
                    try:
                        temp_data = json.load(f)
                    except json.JSONDecodeError:
                        continue

                merge_dicts(lang_data, temp_data)

                with open(lang_file_path, 'w', encoding='utf-8') as f:
                    json.dump(lang_data, f, ensure_ascii=False, indent=4)
                
                display_name = os.path.join(rel_path, file) if rel_path != "." else file
                print(f"[<-] Applied translations to: {display_name}")

    shutil.rmtree(temp_dir)
    print(f"Cleaned up temporary translation files for '{args.lang_folder}'.")
    
    if os.path.exists(base_temp_dir) and not os.listdir(base_temp_dir):
        os.rmdir(base_temp_dir)

if __name__ == "__main__":
    main()
