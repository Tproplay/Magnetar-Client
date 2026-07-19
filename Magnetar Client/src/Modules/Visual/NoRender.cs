using HarmonyLib;
using Il2CppSystem;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class NoRender : Module
    {
        public override string Name { get; set; } = "No Render";
        public override string Description { get; set; } = "Now you can see your lawn.";
        public override string SearchHints { get; set; } = "norender blank lawn invisible invisibleplants novisual" +
            " clear lawn invisiblezombies hiderender hidedisplay nographics seelawn hidden lawnclear hidetexture " +
            "graphicsoff renderdisable norendering invisibletextures clearfield plantshide zombieshide blankfield " +
            "hidelawn seebackground hiderenderer seeground norend hidedraw";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        public static NoRender instance;

        public MultiSelectSetting ParticlesSetting;
        public MultiSelectSetting GameObjectsSetting;
        public MultiSelectSetting BulletSetting;
        public MultiSelectSetting EffectSetting;

        public BoolSetting ScreenShakeSetting;

        private Dictionary<int, string> fxDatabase = new Dictionary<int, string>();
        private string filePath;
        private int nextId = 0;

        public enum ParticleTypes { Empty }
        public NoRender()
        {
            instance = this;

            CreateCategory("General");

            #region Particle
            // 1. Setup Path & Ensure Directory Exists
            string dirPath = Path.Combine(Magnetar_Client.Core.main.ModsDirectory, "Magnetar Data");
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            filePath = Path.Combine(dirPath, "FxData.json");

            Dictionary<int, string> menuNames = new Dictionary<int, string>();

            // 2. Load the JSON
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
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

            // 3. Menu
            ParticlesSetting = new MultiSelectSetting("Particles", typeof(ParticleTypes))
            {
                CustomNames = menuNames
            };

            AddSettings(ParticlesSetting);
            #endregion

            GameObjectsSetting = new MultiSelectSetting("Game Objects", typeof(BucketType))
            {
                CustomNames = TranslatedNames(typeof(BucketType))
            };
            AddSettings(GameObjectsSetting);

            BulletSetting = new MultiSelectSetting("Bullets", typeof(BulletType))
            {
                CustomNames = TranslatedNames(typeof(BulletType))
            };
            AddSettings(BulletSetting);

            EffectSetting = new MultiSelectSetting("Effects")
            {
                Options = new Dictionary<int, string>
                {
                    { 1, "Ice shroom effect" },
                    { 2, "Doom shroom effect" },
                }
            };

            AddSettings(EffectSetting);
            EndCategory();

            CreateCategory("Extra");

            ScreenShakeSetting = new BoolSetting("Disable Screen Shake effect", false);

            AddSettings(ScreenShakeSetting);
            EndCategory();

        }

        public override void OnLanguageChanged()
        {
            GameObjectsSetting.CustomNames = TranslatedNames(typeof(BucketType));
            BulletSetting.CustomNames = TranslatedNames(typeof(BulletType));

            EffectSetting.Options = EffectSetting.Options
                .ToDictionary(kvp => kvp.Key, kvp => Translator.Translate(kvp.Value));
        }

        public override void OnUpdateActive()
        {
            if (Game.AppData.BoardInstanceIsNull) return;

            // Particles
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
            List<string> effectsList = new List<string>(fxDatabase.Values);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(effectsList, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
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
                    if (renderer != null && renderer.Pointer != IntPtr.Zero)
                    {
                        renderer.enabled = true;
                    }
                }
            }

            // Bullets

            var Bullets = GameObject.FindObjectsOfType<Bullet>();

            foreach (var bullet in Bullets)
            {
                var renderers = bullet.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.Pointer != IntPtr.Zero)
                    {
                        renderer.enabled = true;
                    }
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
                        if (renderer != null && renderer.Pointer != IntPtr.Zero)
                        {
                            renderer.enabled = false;
                        }
                    }
                }
            }

            // Bullets

            var Bullets = GameObject.FindObjectsOfType<Bullet>();

            foreach (var bullet in Bullets)
            {
                if (BulletSetting.IsSelected((int)bullet.theBulletType))
                {
                    var renderers = bullet.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        if (renderer != null && renderer.Pointer != IntPtr.Zero)
                        {
                            renderer.enabled = false;
                        }
                    }
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
            [HarmonyPatch(nameof(Bullet.InitData))]
            [HarmonyPostfix]
            public static void InitDataPatch(Bullet __instance)
            {
                if (instance == null || !instance.Active || 
                    !instance.BulletSetting.IsSelected((int)__instance.theBulletType)) return;

                var comp = __instance.GetComponentsInChildren<Renderer>(true);

                foreach (var renderer in comp)
                {
                    if (renderer != null && renderer.Pointer != IntPtr.Zero)
                    {
                        renderer.enabled = false;
                    }
                }
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
                if (instance==null || !instance.Active || !instance.EffectSetting.IsSelected(2)) return;

                var effect = __instance.transform.Find("sprit");
                if (effect != null)
                {
                    effect.gameObject.active = false;
                }
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
    }
}