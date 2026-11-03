## Localization Workflow Scripts

A set of automated Python utility scripts designed to manage, sync, and update localized JSON files for Magnetar Client. These tools help manage structure discrepancies, missing files, and untranslated content between a baseline language folder (English) and multiple target language variants.

------------------------------

## Folder Layout

The scripts expect to be placed inside the root directory alongside your localization folders:

```
📁 Magnetar Translation/
├── 📄 sync_language.py
├── 📄 export_untranslated.py
├── 📄 apply_translations.py
├── 📁 English/
└── 📁 Spanish/
```

------------------------------

## 1. Synchronization Tool (sync_language.py)

This script structures your target localization directories based on the templates found in the English directory. It walks through all subdirectories, mirroring structural updates, additions, and removals.

## Operations

* Additions: Copies missing files and newly introduced json keys into target folders.
* Preservation: Skips keys already translated in the destination folders, ensuring current localization work is never overwritten.
* Enforcement: Corrects structural data type mismatches (e.g., if a key changes from a string to a nested object in the baseline file).
* Removals: Automatically deletes keys and individual files from target folders if they no longer exist in the English directory.
* Logging: Generates terminal alerts displaying operations ([+] Added, [-] Removed, [*] Type mismatched, or Created new file).

## Run Command

```bash
py sync_language.py <target_language_folder>
```

Example:

```bash
python sync_language.py Spanish
```

------------------------------

## 2. Extraction Tool (export_untranslated.py)

Scans a designated target language folder to locate placeholder text where localization has not yet occurred (identifies any instance where key == value).

## Operations

* Isolation: Extracts fields flagged as untranslated while maintaining their full object hierarchies and original nested structures.
* Output: Saves the filtered contents into a newly generated temp_untranslated/<target_language_folder>/ folder.
* Non-destructive: Does not change or modify your actual production localization data.

## Run Command

```bash
python export_untranslated.py <target_language_folder>
```

Example: 

```bash
python export_untranslated.py Spanish
```

------------------------------

## 3. Integration Tool (apply_translations.py)

Processes files previously exported into the temp_untranslated folder once they have been updated by translators.

## Operations

* Merge: Re-injects the corrected translation mappings directly back into the target production directories.
* Cleanup: Deletes the temporary working structures and files located inside temp_untranslated upon successful execution.

## Run Command

```bash
python apply_translations.py <target_language_folder>
```

Example: 

```bash
python apply_translations.py Spanish
```

------------------------------

## Workflow Guide

   1. Run sync_language.py to incorporate new keys or structural shifts from production updates.
   2. Run export_untranslated.py to isolate entries that require manual or automated machine translation.
   3. Apply translations directly to the generated isolated files located in the temp_untranslated/ directory.
   4. Run apply_translations.py to compile the localized strings back into the production build and purge the working staging environment.

