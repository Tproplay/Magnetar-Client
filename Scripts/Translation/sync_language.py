import os
import json
import argparse

def sync_dicts(eng_data, lang_data, path=""):
    log_messages = []
    
    keys_to_remove = [key for key in lang_data if key not in eng_data]
    for key in keys_to_remove:
        del lang_data[key]
        full_path = f"{path}.{key}" if path else key
        log_messages.append(f"  [-] Removed key: {full_path}")
    
    for key, eng_val in eng_data.items():
        full_path = f"{path}.{key}" if path else key
        if key not in lang_data:
            lang_data[key] = eng_val
            log_messages.append(f"  [+] Added missing key: {full_path}")
        else:
            if isinstance(eng_val, dict) and isinstance(lang_data[key], dict):
                nested_logs = sync_dicts(eng_val, lang_data[key], full_path)
                log_messages.extend(nested_logs)
            elif type(eng_val) != type(lang_data[key]):
                lang_data[key] = eng_val
                log_messages.append(f"  [*] Updated structural type mismatch for key: {full_path}")
                
    return log_messages

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("lang_folder")
    args = parser.parse_args()

    script_dir = os.path.dirname(os.path.abspath(__file__))
    eng_dir = os.path.join(script_dir, "English")
    lang_dir = os.path.join(script_dir, args.lang_folder)

    if not os.path.exists(eng_dir):
        print(f"Error: 'English' folder not found at {eng_dir}")
        return

    print(f"--- Synchronizing '{args.lang_folder}' with 'English' ---")

    for root, dirs, files in os.walk(eng_dir):
        for file in files:
            if file.endswith('.json'):
                rel_path = os.path.relpath(root, eng_dir)
                target_lang_dir = os.path.normpath(os.path.join(lang_dir, rel_path))
                
                eng_file_path = os.path.join(root, file)
                lang_file_path = os.path.join(target_lang_dir, file)

                os.makedirs(target_lang_dir, exist_ok=True)

                with open(eng_file_path, 'r', encoding='utf-8') as f:
                    eng_data = json.load(f)

                is_new_file = not os.path.exists(lang_file_path)
                if not is_new_file:
                    with open(lang_file_path, 'r', encoding='utf-8') as f:
                        try:
                            lang_data = json.load(f)
                        except json.JSONDecodeError:
                            lang_data = {}
                else:
                    lang_data = {}

                display_name = os.path.join(rel_path, file) if rel_path != "." else file
                
                if is_new_file:
                    print(f"\n[+] Created new file: {display_name}")
                    updated_data = eng_data
                else:
                    file_logs = sync_dicts(eng_data, lang_data)
                    if file_logs:
                        print(f"\n[*] Updating file: {display_name}")
                        for log in file_logs:
                            print(log)
                    updated_data = lang_data

                with open(lang_file_path, 'w', encoding='utf-8') as f:
                    json.dump(updated_data, f, ensure_ascii=False, indent=4)

    if os.path.exists(lang_dir):
        for root, dirs, files in os.walk(lang_dir, topdown=False):
            for file in files:
                if file.endswith('.json'):
                    rel_path = os.path.relpath(root, lang_dir)
                    eng_file_path = os.path.normpath(os.path.join(eng_dir, rel_path, file))
                    
                    if not os.path.exists(eng_file_path):
                        os.remove(os.path.join(root, file))
                        display_name = os.path.join(rel_path, file) if rel_path != "." else file
                        print(f"\n[-] Deleted obsolete file: {display_name}")
            
            if not os.listdir(root):
                os.rmdir(root)

if __name__ == "__main__":
    main()
