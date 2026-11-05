# Frequently Asked Questions (FAQ)

Find answers to common questions, compatibility details, and troubleshooting steps for **Magnetar Client** and its addons.

---

### 1. My plants are invincible even though God Mode is off, or plants stack without Plant Anywhere enabled. How do I fix this?
**Answer:**  
This issue occurs when your game version does not match the mod release version. Game assemblies and memory structures change with every game update.
* **All supported game versions are listed on the [GitHub Releases](https://github.com/Tproplay/Magnetar-Client/releases) page of the specific version.**
* Always ensure your installed game version matches the supported version listed on the release notes (e.g. Magnetar Client 4.1.0 is built for PVZ Fusion 4.1).

---

### 2. Audio Addon is showing an error or fails to download the soundtrack automatically. Where can I download audio files manually?
**Answer:**  
By default, Audio Addon attempts to download the default soundtrack AssetBundle (`musicAB_windows` on PC, `musicAB_android` on Android) in the background from GitHub raw servers. If your firewall, network connection, or ISP blocks GitHub raw downloads, you can download the bundle manually:

1. **Download the AssetBundle for your platform:**
   * **PC (MelonLoader & BepInEx):** [Download `musicAB_windows`](https://github.com/Tproplay/Magnetar-Client/raw/master/Addons/Audio%20Addon/Project/Custom%20Audios/musicAB_windows)
   * **Android:** [Download `musicAB_android`](https://github.com/Tproplay/Magnetar-Client/raw/master/Addons/Audio%20Addon/Project/Custom%20Audios/musicAB_android)
   * Or browse all sound packs in the [Custom Audios Repository Folder](https://github.com/Tproplay/Magnetar-Client/tree/master/Addons/Audio%20Addon/Project/Custom%20Audios).

2. **Place the file into the `Custom Audios/Music/` directory:**
   * **MelonLoader:**  
     `[Game Directory]/Mods/Magnetar Data/Addons/Custom Audios/Music/musicAB_windows`
   * **BepInEx:**  
     `[Game Directory]/BepInEx/plugins/Magnetar Data/Addons/Custom Audios/Music/musicAB_windows`
   * **Android:**  
     `plugins/Magnetar Data/Addons/Custom Audios/Music/musicAB_android`

*(Create the `Custom Audios` and `Music` folders if they do not already exist).*

---

### 3. Is Simple Spawner part of Magnetar Client or a separate mod?
**Answer:**  
[**Simple Spawner**](https://github.com/Tproplay/Simple-Spawner) is a standalone, independent mod project maintained at [https://github.com/Tproplay/Simple-Spawner](https://github.com/Tproplay/Simple-Spawner).  
It is included out-of-the-box in the PC **FULL** packages (`-FULL.zip`) purely for user convenience so you have a complete utility suite ready to go. You can update or replace `SimpleSpawner.dll` at any time from its dedicated repository.

---

### 4. What is the difference between Assist Bot Online and Offline?
**Answer:**  
* **Online Variant:** Automatically syncs the latest neural prediction model on game startup from the server and uploads anonymized telemetry after completed runs to retrain the model.
* **Offline Variant:** Never connects to the internet. Comes pre-packaged with `buff_model.zip` (placed in `.../Magnetar Data/Addons/ML/buff_model.zip`) for completely air-gapped or offline play.

---

### 5. MelonLoader: Game crashes or UI conflicts when installing on Multi-Language PVZ Fusion.
**Answer:**  
If you are running the multi-language version of Plants vs. Zombies Fusion, remove `Blooms_QOL.dll` from your `Mods/` folder if it exists. Its internal hooks conflict with Magnetar Client's UI and Il2Cpp patches.
