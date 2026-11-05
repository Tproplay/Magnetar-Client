using HarmonyLib;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class SandboxUI : Module
{
    // Mod Info
    public override string Name { get; set; } = "Sandbox UI";
    public override string Description { get; set; } = "Replaces the current in-game level UI with the IZE / EVE UI" +
        " (with zombie seed bank, select zombie button, and bottom EVE toolbar). Restores the normal UI when disabled.";
    public override string SearchHints { get; set; } = "sandboxui izeui eveui zombieseeds selectzombie evetoolbar levelui " +
        "uireplacer sandboxmode editorui customui uimod interfacechanger zombieseedbank sandboxinterface izeinterface " +
        "eveinterface uioverride levelinterface uimanager custominterface"; 
    public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

    // Mod Data
    public static SandboxUI instance;

    public BoolSetting EnableIZTag;

    private GameObject spawnedMainUI;
    private GameObject spawnedSubUI;
    private bool customUIApplied;
    private bool wasOriginalIZ;

    private static readonly HashSet<string> RedundantObjectNames = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "LevelName",
        "MoneyBank",
        "SlowTrigger",
        "BackToMenu",
        "BackToMainMenu"
    };

    public SandboxUI()
    {
        instance = this;

        CreateCategory("General");

        EnableIZTag = new BoolSetting("Set Board isIZ flag (Enables zombie placement)", true);

        AddSettings(EnableIZTag);
        EndCategory();
    }

    public void ResetState()
    {
        spawnedMainUI = null;
        spawnedSubUI = null;
        customUIApplied = false;
        wasOriginalIZ = false;
    }

    public override void OnEnable()
    {
        if (Board.Instance != null && InGameUI.Instance != null)
        {
            ApplyCustomUI();
        }
    }

    public override void OnDisable()
    {
        RevertCustomUI();
    }

    public override void OnUpdateActive()
    {
        // If not in level or board destroyed, clean up any lingering UI immediately
        if (GameData.BoardInstanceIsNull || Board.Instance == null)
        {
            if (customUIApplied || spawnedMainUI != null || spawnedSubUI != null)
            {
                DestroySpawnedUI();
                ResetState();
            }
            return;
        }

        if (!customUIApplied)
        {
            if (InGameUI.Instance != null && InGameUI.Instance.gameObject != null && InGameUI.Instance.gameObject.activeInHierarchy)
            {
                ApplyCustomUI();
            }
        }
        else
        {
            // Keep default InGameUI hidden while custom UI is active
            if (InGameUI.Instance != null && InGameUI.Instance.gameObject != null && InGameUI.Instance.gameObject.activeSelf)
            {
                InGameUI.Instance.gameObject.SetActive(false);
            }
        }
    }

    public void ApplyCustomUI()
    {
        if (Board.Instance == null) return;

        // Clean up any existing instances first
        DestroySpawnedUI();

        // 1. Hide the original InGameUI
        if (InGameUI.Instance != null && InGameUI.Instance.gameObject != null)
        {
            InGameUI.Instance.gameObject.SetActive(false);
        }

        // 2. Set Board Tag for IZE mode if needed
        if (EnableIZTag.Value)
        {
            var tag = Board.Instance.boardTag;
            wasOriginalIZ = tag.isIZ;
            tag.isIZ = true;
            Board.Instance.boardTag = tag;
        }

        // 3. Instantiate Main UI (InGameUI_IZ) on GameAPP.canvas
        Transform canvas = GameAPP.canvas;
        Transform canvasUp = GameAPP.canvasUp;

        if (canvas != null)
        {
            spawnedMainUI = SpawnMenuPrefab(UIType.InGameUI_IZ, canvas);
            if (spawnedMainUI != null)
            {
                RemoveRedundantElements(spawnedMainUI);
            }
        }

        // 4. Instantiate Sub UI (IZBottomMenu) on GameAPP.canvasUp
        if (canvasUp != null)
        {
            spawnedSubUI = SpawnMenuPrefab(UIType.IZBottomMenu, canvasUp);
            if (spawnedSubUI != null)
            {
                RemoveRedundantElements(spawnedSubUI);
            }
        }

        customUIApplied = true;
    }

    private static void RemoveRedundantElements(GameObject root)
    {
        if (root == null) return;

        var toDestroy = new List<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child != root.transform && RedundantObjectNames.Contains(child.name))
            {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach (var obj in toDestroy)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                UnityEngine.Object.Destroy(obj);
            }
        }
    }

    public void DestroySpawnedUI()
    {
        if (spawnedMainUI != null)
        {
            UnityEngine.Object.Destroy(spawnedMainUI);
            spawnedMainUI = null;
        }

        if (spawnedSubUI != null)
        {
            UnityEngine.Object.Destroy(spawnedSubUI);
            spawnedSubUI = null;
        }

        // Clean up any lingering or orphaned instances
        var lingeringMain = UnityEngine.Object.FindObjectsOfType<InGameUI_IZ>();
        foreach (var ui in lingeringMain)
        {
            if (ui != null && ui.gameObject != null)
            {
                UnityEngine.Object.Destroy(ui.gameObject);
            }
        }

        var lingeringSub = UnityEngine.Object.FindObjectsOfType<IZBottomMenu>();
        foreach (var ui in lingeringSub)
        {
            if (ui != null && ui.gameObject != null)
            {
                UnityEngine.Object.Destroy(ui.gameObject);
            }
        }

        customUIApplied = false;
    }

    public void RevertCustomUI()
    {
        // 1. Destroy spawned custom menus
        DestroySpawnedUI();

        // 2. Restore original board isIZ tag
        if (Board.Instance != null && wasOriginalIZ != Board.Instance.boardTag.isIZ)
        {
            var tag = Board.Instance.boardTag;
            tag.isIZ = wasOriginalIZ;
            Board.Instance.boardTag = tag;
        }

        // 3. Restore default InGameUI
        if (InGameUI.Instance != null && InGameUI.Instance.gameObject != null)
        {
            InGameUI.Instance.gameObject.SetActive(true);
        }

        ResetState();
    }

    private static GameObject SpawnMenuPrefab(UIType uiType, Transform parent)
    {
        GameObject prefab = null;

        // Check if preloaded in UI Manager
        if (GameAPP.UIManager != null && GameAPP.UIManager.UIPrefabs != null)
        {
            GameAPP.UIManager.UIPrefabs.TryGetValue(uiType, out prefab);
        }

        // Fallback to Resources
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>($"UI/Prefabs/{uiType}");
        }

        if (prefab != null && parent != null)
        {
            GameObject obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.name = prefab.name;
            return obj;
        }

        return null;
    }

    [HarmonyPatch(typeof(Board))]
    public static class BoardPatches
    {
        [HarmonyPatch(nameof(Board.Awake))]
        [HarmonyPostfix]
        public static void AwakePostfix()
        {
            if (instance != null)
            {
                instance.DestroySpawnedUI();
                instance.ResetState();
            }
        }

        [HarmonyPatch(nameof(Board.OnDestroy))]
        [HarmonyPrefix]
        public static void OnDestroyPrefix()
        {
            if (instance != null)
            {
                instance.DestroySpawnedUI();
                instance.ResetState();
            }
        }

        [HarmonyPatch(nameof(Board.Die))]
        [HarmonyPostfix]
        public static void DiePostfix()
        {
            if (instance != null)
            {
                instance.DestroySpawnedUI();
                instance.ResetState();
            }
        }
    }

    [HarmonyPatch(typeof(UIMgr))]
    public static class UIMgrPatches
    {
        [HarmonyPatch(nameof(UIMgr.BackToMenu))]
        [HarmonyPrefix]
        public static void BackToMenuPrefix()
        {
            if (instance != null)
            {
                instance.DestroySpawnedUI();
                instance.ResetState();
            }
        }
    }
}
