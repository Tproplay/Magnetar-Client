import os
import json
import argparse


def resolve_base_dir() -> str:
    script_dir = os.path.dirname(os.path.abspath(__file__))
    candidate = os.path.normpath(os.path.join(script_dir, "..", "..", "Magnetar Client", "Magnetar Data", "Magnetar Translation"))
    if os.path.isdir(os.path.join(candidate, "English")):
        return candidate
    return script_dir


def filter_untranslated(data, eng_data=None):
    filtered = {}
    for key, val in data.items():
        eng_val = eng_data.get(key) if isinstance(eng_data, dict) else None
        if isinstance(val, dict):
            nested = filter_untranslated(val, eng_val if isinstance(eng_val, dict) else None)
            if nested:
                filtered[key] = nested
        elif isinstance(val, str):
            # Flagged as untranslated if key == val, or if identical to English counterpart
            is_untranslated = (key == val) or (
                isinstance(eng_val, str)
                and val == eng_val
                and val.strip() != ""
                and not val.strip().isdigit()
            )
            if is_untranslated:
                filtered[key] = val
    return filtered


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("lang_folder")
    args = parser.parse_args()

    base_dir = resolve_base_dir()
    eng_dir = os.path.join(base_dir, "English")
    lang_dir = os.path.join(base_dir, args.lang_folder)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    temp_dir = os.path.join(script_dir, "temp_untranslated", args.lang_folder)

    if not os.path.exists(lang_dir):
        print(f"Error: Folder '{args.lang_folder}' not found at {lang_dir}.")
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

                eng_file_path = os.path.normpath(os.path.join(eng_dir, rel_path, file))
                eng_data = None
                if os.path.exists(eng_file_path):
                    with open(eng_file_path, 'r', encoding='utf-8') as ef:
                        try:
                            eng_data = json.load(ef)
                        except json.JSONDecodeError:
                            eng_data = None

                untranslated_data = filter_untranslated(data, eng_data)

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
