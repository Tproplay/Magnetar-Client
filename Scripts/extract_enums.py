import json
import os
import re

TAG_RE = re.compile(r"<[^>]+>")


def clean_name(name: str) -> str:
    """Strips HTML/RichText tags and trims surrounding whitespace."""
    if not name:
        return ""
    cleaned = TAG_RE.sub("", name)
    return cleaned.strip()


def convert_plants(
    input_file: str = "LawnStringsTranslate.json",
    output_file: str = "PlantType.json",
):
    if not os.path.exists(input_file):
        print(f"[Warning] '{input_file}' not found. Skipping PlantType generation.")
        return

    with open(input_file, "r", encoding="utf-8") as f:
        data = json.load(f)

    plant_dict = {}
    for item in data.get("plants", []):
        if "seedType" in item and "name" in item:
            seed_type = int(item["seedType"])
            name = clean_name(item["name"])
            plant_dict[seed_type] = name

    # Sort numerically by key
    sorted_plants = {
        str(k): plant_dict[k] for k in sorted(plant_dict.keys())
    }

    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(sorted_plants, f, indent=4, ensure_ascii=False)

    print(
        f"[Success] Generated '{output_file}' with {len(sorted_plants)} entries."
    )


def convert_zombies(
    input_file: str = "ZombieStringsTranslate.json",
    output_file: str = "ZombieType.json",
):
    if not os.path.exists(input_file):
        print(
            f"[Warning] '{input_file}' not found. Skipping ZombieType generation."
        )
        return

    with open(input_file, "r", encoding="utf-8") as f:
        data = json.load(f)

    zombie_dict = {}
    for item in data.get("zombies", []):
        if "theZombieType" in item and "name" in item:
            zombie_type = int(item["theZombieType"])
            name = clean_name(item["name"])
            zombie_dict[zombie_type] = name

    # Sort numerically by key
    sorted_zombies = {
        str(k): zombie_dict[k] for k in sorted(zombie_dict.keys())
    }

    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(sorted_zombies, f, indent=4, ensure_ascii=False)

    print(
        f"[Success] Generated '{output_file}' with {len(sorted_zombies)} entries."
    )


if __name__ == "__main__":
    convert_plants()
    convert_zombies()