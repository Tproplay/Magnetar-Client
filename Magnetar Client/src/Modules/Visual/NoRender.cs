using HarmonyLib;
using Magnetar_Client.UI.Setting;
using Il2CppSystem;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Magnetar_Client.Api;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class NoRender : Module
{
    // Mod Info
    public override string Name { get; set; } = "No Render";
    public override string Description { get; set; } = "Now you can see your lawn.";
    public override string SearchHints { get; set; } = "norender blank lawn invisible invisibleplants novisual" +
        " clear lawn invisiblezombies hiderender hidedisplay nographics seelawn hidden lawnclear hidetexture " +
        "graphicsoff renderdisable norendering invisibletextures clearfield plantshide zombieshide blankfield " +
        "hidelawn seebackground hiderenderer seeground norend hidedraw no particles remove particles particletypes remove" +
        "particles disable particles clear particles";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;
    public override bool EnableInVanillaMode { get; set; } = true;

    // Mod Data
    public static NoRender instance;

    public MultiSelectSetting ParticlesSetting;
    public MultiSelectSetting ParticleTypeSetting;
    public MultiSelectSetting GameObjectsSetting;
    public MultiSelectSetting BulletSetting;
    public MultiSelectSetting EffectSetting;
    public MultiSelectSetting OtherSetting;

    public BoolSetting ScreenShakeSetting;

    private Dictionary<int, string> fxDatabase = new();
    private int nextId;

    public NoRender()
    {
        instance = this;

        var strings = new string[]
        {
            "Ice shroom effect", "Doom shroom effect", "Jalapeno fire line", "Doom shroom smoke cloud", "Falling Sun",
            "Falling Coin",
        };

        RegisterTranslations(Translator.CreateDictionary(strings));

        CreateCategory("General");

        Dictionary<int, string> menuNames = new();

        if (File.Exists(PathsManager.FXDataPath))
        {
            try
            {
                string json = File.ReadAllText(PathsManager.FXDataPath);
                List<string> loadedEffects = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);

                if (loadedEffects != null)
                {
                    foreach (string effect in loadedEffects)
                    {
                        fxDatabase.Add(nextId, effect);
                        menuNames.Add(nextId, effect);
                        nextId++;
                    }
                }
            }
            catch { }
        }

        if (menuNames.Count == 0) menuNames[-1] = "Empty";

        ParticlesSetting = new MultiSelectSetting("Particles")
        {
            Options = menuNames,
            OnSelectionChanged = (effectId, selected) =>
            {
                if (!Active || GameData.BoardInstanceIsNull) return;
                foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
                {
                    if (ps == null || ps.gameObject == null) continue;
                    string psName = ps.gameObject.name;
                    if (psName.EndsWith("(Clone)")) psName = psName.Substring(0, psName.Length - 7);
                    bool dirty = false;
                    if (GetOrRegisterEffect(psName, ref dirty) != effectId) continue;
                    ps.emission.enabled = !selected;
                    if (selected) ps.Clear();
                }
            }
        };

        ParticleTypeSetting = new MultiSelectSetting("Particle Types", typeof(ParticleType))
        {
            CustomNames = TranslateEnum(typeof(ParticleType))
        };

        GameObjectsSetting = new MultiSelectSetting("Game Objects", typeof(BucketType))
        {
            CustomNames = TranslateEnum(typeof(BucketType)),
            OnSelectionChanged = (bucketTypeId, selected) =>
            {
                if (!Active || GameData.BoardInstanceIsNull) return;
                foreach (var bucket in GameObject.FindObjectsOfType<Bucket>())
                {
                    if (bucket == null || (int)bucket.theBucketType != bucketTypeId) continue;
                    foreach (var r in bucket.GetComponentsInChildren<Renderer>(true))
                        if (r != null) r.enabled = !selected;
                }
            }
        };

        BulletSetting = new MultiSelectSetting("Bullets", typeof(BulletType))
        {
            CustomNames = TranslateEnum(typeof(BulletType)),
            OnSelectionChanged = (bulletTypeId, selected) =>
            {
                if (!Active || GameData.BoardInstanceIsNull) return;
                foreach (var kv in BulletPatch.RendererCache)
                {
                    if (kv.Key == null || (int)kv.Key.theBulletType != bulletTypeId) continue;
                    foreach (var r in kv.Value)
                        if (r != null) r.enabled = !selected;
                }
            }
        };

        EffectSetting = new MultiSelectSetting("Effects")
        {
            Options = new Dictionary<int, string>
            {
                { 1, "Ice shroom effect" },
                { 2, "Doom shroom effect" },
                { 3, "Jalapeno fire line" },
                { 4, "Doom shroom smoke cloud" }
            },
            OnSelectionChanged = (effectId, selected) =>
            {
                if (!Active || GameData.BoardInstanceIsNull) return;
                if (effectId == 1)
                {
                    foreach (var ice in GameObject.FindObjectsOfType<IceExplodeControl>())
                    {
                        if (ice == null) continue;
                        var sr = ice.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.enabled = !selected;
                    }
                }
                else if (effectId == 2 || effectId == 4)
                {
                    foreach (var doom in GameObject.FindObjectsOfType<Doom>())
                    {
                        if (doom == null) continue;
                        if (effectId == 2)
                        {
                            var sprit = doom.transform.Find("sprit");
                            if (sprit != null) sprit.gameObject.active = !selected;
                        }
                        else
                        {
                            for (int i = 0; i < doom.transform.childCount; i++)
                            {
                                Transform child = doom.transform.GetChild(i);
                                if (child != null && child.gameObject != null)
                                    child.gameObject.active = !selected;
                            }
                        }
                    }
                }
            }
        };

        OtherSetting = new MultiSelectSetting("Others")
        {
            Options = new Dictionary<int, string>
            {
                { 0, "Falling Sun" },
                { 1, "Falling Coin" },
            },
            OnSelectionChanged = (otherId, selected) =>
            {
                if (!Active || GameData.BoardInstanceIsNull) return;
                if (otherId == 0)
                {
                    foreach (var sun in GameObject.FindObjectsOfType<CoinSun>())
                    {
                        if (sun == null) continue;
                        foreach (var sr in sun.GetComponentsInChildren<SpriteRenderer>())
                            if (sr != null) sr.enabled = !selected;
                    }
                }
                else if (otherId == 1)
                {
                    foreach (var coin in GameObject.FindObjectsOfType<CoinMoney>())
                    {
                        if (coin == null) continue;
                        foreach (var sr in coin.GetComponentsInChildren<SpriteRenderer>())
                            if (sr != null) sr.enabled = !selected;
                    }
                }
            }
        };

        AddSettings(ParticlesSetting, ParticleTypeSetting, GameObjectsSetting, BulletSetting, EffectSetting, OtherSetting);
        EndCategory();

        CreateCategory("Extra");

        ScreenShakeSetting = new BoolSetting("Disable Screen Shake effect", false);

        AddSettings(ScreenShakeSetting);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        GameObjectsSetting.CustomNames = TranslateEnum(typeof(BucketType));
        BulletSetting.CustomNames = TranslateEnum(typeof(BulletType));

        EffectSetting.CustomNames = EffectSetting.Options
            .ToDictionary(kvp => kvp.Key, kvp => Translate(kvp.Value));
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        // Particles
        if (ParticlesSetting.SelectedValues.Count != 0)
        {
            bool isFileDirty = false;
            var allParticleSystems = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();

            foreach (var ps in allParticleSystems)
            {
                if (ps == null || ps.gameObject == null) continue;

                string name = ps.gameObject.name;
                if (name.EndsWith("(Clone)")) name = name.Substring(0, name.Length - 7);

                // Fetch the ID, and automatically register it if it is new
                int effectId = GetOrRegisterEffect(name, ref isFileDirty);

                if (ParticlesSetting.IsSelected(effectId))
                {
                    ps.emission.enabled = false;
                    ps.Clear();
                }
            }

            if (isFileDirty) SaveToJson();
        }
    }

    private int GetOrRegisterEffect(string effectName, ref bool isFileDirty)
    {
        foreach (var pair in fxDatabase)
        {
            if (string.Equals(pair.Value, effectName, System.StringComparison.OrdinalIgnoreCase))
            {
                return pair.Key;
            }
        }

        int newId = nextId++;
        fxDatabase.Add(newId, effectName);
        ParticlesSetting.Options.Add(newId, effectName);

        isFileDirty = true;
        return newId;
    }

    private void SaveToJson()
    {
        List<string> effectsList = new(fxDatabase.Values);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(effectsList, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(PathsManager.FXDataPath, json);
    }

    public override void OnDisable()
    {
        // Particles
        var allParticleSystems = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allParticleSystems)
        {
            if (ps != null && 
                ParticlesSetting.IsSelected(
                    ParticlesSetting.Options.FirstOrDefault(key=>key.Value==ps.gameObject.name).Key
                    )
                ) ps.emission.enabled = true;
        }

        // Buckets
        var Buckets = GameObject.FindObjectsOfType<Bucket>();

        foreach (var bucket in Buckets)
        {
            var renderers = bucket.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }

        // Bullets
        foreach (var bullet in BulletPatch.RendererCache.Keys)
        {
            if (bullet == null) continue;
            foreach (var renderer in BulletPatch.RendererCache[bullet])
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
    }

    public override void OnEnable()
    {
        // Buckets
        var Buckets = GameObject.FindObjectsOfType<Bucket>();

        foreach (var bucket in Buckets)
        {
            if (GameObjectsSetting.IsSelected((int)bucket.theBucketType))
            {
                var renderers = bucket.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }

        // Bullets
        foreach (var bullet in BulletPatch.RendererCache.Keys)
        {
            if (bullet == null || !BulletSetting.IsSelected((int)bullet.theBulletType)) continue;
            foreach (var renderer in BulletPatch.RendererCache[bullet])
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(Bucket))]
    public class BucketPatch
    {
        [HarmonyPatch(nameof(Bucket.Start))]
        [HarmonyPostfix]
        public static void StartPatch(Bucket __instance)
        {
            if (instance == null || !instance.Active ||
                !instance.GameObjectsSetting.IsSelected((int)__instance.theBucketType)) return;

            var renderers = __instance.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }
        }

    }

    [HarmonyPatch(typeof(Bullet))]
    public static class BulletPatch
    {
        public static readonly Dictionary<Bullet, Renderer[]> RendererCache = new();

        [HarmonyPatch(nameof(Bullet.InitData))]
        [HarmonyPostfix]
        public static void InitDataPatch(Bullet __instance)
        {
            if (instance == null) return;

            // Populate cache on first encounter of this Bullet instance.
            if (!RendererCache.TryGetValue(__instance, out var comp))
            {
                comp = __instance.GetComponentsInChildren<Renderer>(true);
                RendererCache[__instance] = comp;
            }

            bool shouldHide = instance.Active && instance.BulletSetting.IsSelected((int)__instance.theBulletType);

            foreach (var renderer in comp)
            {
                if (renderer != null)
                    renderer.enabled = !shouldHide;
            }
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardCacheClearPatch
        {
            [HarmonyPatch(nameof(Board.OnDestroy))]
            [HarmonyPostfix]
            public static void OnDestroyPostfix() => RendererCache.Clear();
        }
    }

    [HarmonyPatch(typeof(IceExplodeControl))]
    public static class IceExplodeControlPatch
    {
        [HarmonyPatch(nameof(IceExplodeControl.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(IceExplodeControl __instance)
        {
            if (instance == null || !instance.Active || __instance==null || !instance.EffectSetting.IsSelected(1)) return;
            
            var effect = __instance.GetComponent<SpriteRenderer>();
            if (effect != null)
            {
                effect.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(Doom))]
    public static class DoomPatch
    {
        [HarmonyPatch(nameof(Doom.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(Doom __instance)
        {
            if (instance==null || !instance.Active) return;

            if (instance.EffectSetting.IsSelected(2))
            {
                var effect = __instance.transform.Find("sprit");
                if (effect != null)
                {
                    effect.gameObject.active = false;
                }
            }
            if (instance.EffectSetting.IsSelected(4))
            {
                for (int i = 0; i < __instance.transform.childCount; i++)
                {
                    Transform child = __instance.transform.GetChild(i);

                    if (child != null && child.gameObject != null)
                    {
                        child.gameObject.active = false;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(BoardAction))]
    public static class BoardActionPatch
    {
        [HarmonyPatch(nameof(BoardAction.CreateFireAnim))]
        [HarmonyPrefix]
        public static bool CreateFireAnim(GameObject __instance)
        {
            if (instance == null || !instance.Active || !instance.EffectSetting.IsSelected(3)) return true;
            return false;
        }

    }

    [HarmonyPatch(typeof(ScreenShake))]
    public static class ScreenShakePatch
    {
        [HarmonyPatch(nameof(ScreenShake.TriggerShake))]
        [HarmonyPrefix]
        public static bool TriggerShakePrefix()
        {
            if (instance == null || !instance.Active || !instance.ScreenShakeSetting.Value) return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(ParticleManager))]
    public static class ParticleManagerPatch
    {
        [HarmonyPatch(nameof(ParticleManager.SetParticle))]
        [HarmonyPrefix]
        public static void SetParticlePrefix(ParticleType particleType, ref Vector2 position)
        {
            if (instance == null || !instance.Active || !instance.ParticleTypeSetting.IsSelected((int)particleType)) return;

            position = new Vector2(999, 999);
        }
    }

    [HarmonyPatch(typeof(CoinSun))]
    public static class CoinSunPatch
    {
        [HarmonyPatch(nameof(CoinSun.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(CoinSun __instance)
        {
            if (instance == null || !instance.Active || !instance.OtherSetting.IsSelected(0)) return;

            var comps = __instance.GetComponentsInChildren<SpriteRenderer>();
            foreach ( var comp in comps)
            {
                comp.enabled = false;
            }

        }
    }

    [HarmonyPatch(typeof(CoinMoney))]
    public static class CoinMoneyPatch
    {
        [HarmonyPatch(nameof(CoinMoney.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(CoinMoney __instance)
        {
            if (instance == null || !instance.Active || !instance.OtherSetting.IsSelected(1)) return;

            var comps = __instance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var comp in comps)
            {
                comp.enabled = false;
            }

        }
    }

}