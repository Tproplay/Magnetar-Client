**Supported Game version:** `4.1`

## Changelogs

- changed "Titanium" to "Prismatic" in `Gods Evolved Hack`
- New Modules: `Custom Plant Data`, `Custom Zombie Data`, `Custom Recipes`, `Cooldown Display`, `Scratch-Off Hack`, `Auto Scratch`
- Added `StyleSetting` to generalize customizable typography, colors, outlines, and alignment for IMGUI and TextMeshPro

## Bugfixes

- Escape key didn't worked while `Custom Keybind` module was active
- Pressing Escape while a Bind Setting was focused triggered the settings window Escape instead of just unfocusing the Bind Setting
- Menu's show/hide key worked while focused on a text field
- Color tag inside Text fields lead to weird functionality
- Fixed literal `\n` in format strings not creating line breaks in TextMeshPro
- Restored colored curly bracket syntax highlighting in text fields without swallowing rich text tags or misaligning cursor navigation
- Fixed a bug where the Toggle Slow Mode of `Custom Keybind` won't trigger in normal levels
