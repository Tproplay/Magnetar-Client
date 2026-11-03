# Audio Addon

> Magnetar Client Addon developed by Tproplay.

---

## Project Structure

```
Audio Addon/
├── AudioAddon.slnx              # Visual Studio Solution (MelonLoader & BepInEx)
├── update_references.py        # Helper to sync/download DLL references
├── README.md                   # This document
└── Project/
    ├── AudioAddon.csproj        # SDK Project file
    ├── Properties/
    │   └── AssemblyInfo.cs     # Assembly metadata
    ├── References/             # Engine and loader DLL dependencies
    └── Src/                    # C# Source code
```

---

## Building the Addon

Open `AudioAddon.slnx` in **Visual Studio 2022** or **JetBrains Rider**, select your desired configuration, and build.

Or use the .NET CLI:

```bash
# Build for BepInEx (IL2CPP)
dotnet build "Project/AudioAddon.csproj" -c BepInEx

# Build for MelonLoader (IL2CPP)
dotnet build "Project/AudioAddon.csproj" -c MelonLoader
```

The output DLL will be placed in:
* **BepInEx**: `Project/bin/BepInEx/BepInEx/AudioAddon.dll`
* **MelonLoader**: `Project/bin/Melonloader/MelonLoader/AudioAddon.dll`

---

## Updating References

To download or update the latest references from GitHub at any time:

```bash
python update_references.py
```

To force download directly from GitHub:
```bash
python update_references.py --github
```
