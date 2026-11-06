# Video Player

> Magnetar Client Addon developed by Tproplay.

---

## Project Structure

```
Video Player/
├── VideoPlayer.slnx              # Visual Studio Solution (MelonLoader & BepInEx)
├── update_references.py        # Helper to sync/download DLL references
├── README.md                   # This document
└── Project/
    ├── VideoPlayer.csproj        # SDK Project file
    ├── Properties/
    │   └── AssemblyInfo.cs     # Assembly metadata
    ├── References/             # Engine and loader DLL dependencies
    └── Src/                    # C# Source code
```

---

## Building the Addon

Open `VideoPlayer.slnx` in **Visual Studio 2022** or **JetBrains Rider**, select your desired configuration, and build.

Or use the .NET CLI:

```bash
# Build for BepInEx (IL2CPP)
dotnet build "Project/VideoPlayer.csproj" -c BepInEx

# Build for MelonLoader (IL2CPP)
dotnet build "Project/VideoPlayer.csproj" -c MelonLoader
```

The output DLL will be placed in:
* **BepInEx**: `Project/bin/BepInEx/BepInEx/VideoPlayer.dll`
* **MelonLoader**: `Project/bin/MelonLoader/VideoPlayer.dll`

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
