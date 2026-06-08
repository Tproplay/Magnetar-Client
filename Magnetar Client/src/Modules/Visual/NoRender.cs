using HarmonyLib;
using Il2Cpp;
using Il2CppSystem;
using Magnetar_Client.Utils;
using MelonLoader.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        public static HashSet<Bucket> Buckets = new HashSet<Bucket>();

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
            string dirPath = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Data");
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

            EndCategory();

        }

        public override void OnLanguageChanged()
        {
            GameObjectsSetting.CustomNames = TranslatedNames(typeof(BucketType));
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
                if (ps != null) ps.emission.enabled = true;
            }

            // Buckets
            Buckets.RemoveWhere(b => b == null || b.Pointer == IntPtr.Zero || b.gameObject == null);

            // Buckets
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
        }

        public override void OnEnable()
        {
            // Buckets
            Buckets.RemoveWhere(b => b == null || b.Pointer == IntPtr.Zero || b.gameObject == null);

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
        }

        [HarmonyPatch(typeof(Bucket))]
        public class BucketPatch
        {
            [HarmonyPatch(nameof(Bucket.Start))]
            [HarmonyPostfix]
            public static void StartPatch(Bucket __instance)
            {
                Buckets.Add(__instance);


                if (instance == null || !instance.Active ||
                    !instance.GameObjectsSetting.IsSelected((int)__instance.theBucketType)) return;

                var renderers = __instance.GetComponentsInChildren<Renderer>();

                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }

            [HarmonyPatch(nameof(Bucket.Die))]
            [HarmonyPostfix]
            public static void DiePatch(Bucket __instance)
            {
                if (__instance!=null && Buckets.Contains(__instance))
                {
                    Buckets.Remove(__instance);
                }
            }
        }
    }
}