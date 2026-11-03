import os
import json
import argparse

def filter_untranslated(data):
    filtered = {}
    for key, val in data.items():
        if isinstance(val, dict):
            nested = filter_untranslated(val)
            if nested:
                filtered[key] = nested
        elif isinstance(val, str) and key == val:
            filtered[key] = val
    return filtered

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("lang_folder")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    lang_dir = os.path.join(script_dir, args.lang_folder)
    temp_dir = os.path.join(script_dir, "temp_untranslated", args.lang_folder)

    if not os.path.exists(lang_dir):
        print(f"Error: Folder '{args.lang_folder}' not found.")
        return

    print(f"--- Exporting untranslated strings from '{args.lang_folder}' ---")
    exported_count = 0

    for root, dirs, files in os.walk(lang_dir):
        for file in files:
            if file.endswith('.json'):
                file_path = os.path.join(root, file)
                rel_path = os.path.relpath(root, lang_dir)
                
                with open(file_path, 'r', encoding='utf-8') as f:
                    try:
                        data = json.load(f)
                    except json.JSONDecodeError:
                        continue

                untranslated_data = filter_untranslated(data)
                
                if untranslated_data:
                    target_temp_dir = os.path.normpath(os.path.join(temp_dir, rel_path))
                    os.makedirs(target_temp_dir, exist_ok=True)
                    
                    output_file_path = os.path.join(target_temp_dir, file)
                    with open(output_file_path, 'w', encoding='utf-8') as f:
                        json.dump(untranslated_data, f, ensure_ascii=False, indent=4)
                    
                    display_name = os.path.join(rel_path, file) if rel_path != "." else file
                    print(f"[->] Exported untranslated items to temp folder for: {display_name}")
                    exported_count += 1

    if exported_count == 0:
        print("No untranslated strings found.")
    else:
        print(f"Done. Files are ready for translation inside: temp_untranslated/{args.lang_folder}/")

if __name__ == "__main__":
    main()
