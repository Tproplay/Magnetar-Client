using UnityEngine;
using System.Collections.Generic;
using System.IO;
using MelonLoader.Utils;

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
        public MultiSelectSetting NoRenderParticlesSetting;

        private Dictionary<int, string> fxDatabase = new Dictionary<int, string>();
        private string filePath;
        private int nextId = 0;

        public NoRender()
        {
            instance = this;

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
            NoRenderParticlesSetting = new MultiSelectSetting("Particles", typeof(ParticleTypes))
            {
                CustomNames = menuNames
            };

            AddSettings(NoRenderParticlesSetting);
        }

        public override void OnUpdateActive()
        {
            if (Game.AppData.BoardInstanceIsNull) return;

            bool isFileDirty = false;
            var allParticleSystems = Object.FindObjectsOfType<ParticleSystem>();

            foreach (var ps in allParticleSystems)
            {
                if (ps == null || ps.gameObject == null) continue;

                string name = ps.gameObject.name;
                if (name.EndsWith("(Clone)")) name = name.Substring(0, name.Length - 7);

                // Fetch the ID, and automatically register it if it is new
                int effectId = GetOrRegisterEffect(name, ref isFileDirty);

                if (NoRenderParticlesSetting.IsSelected(effectId))
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
            var allParticleSystems = Object.FindObjectsOfType<ParticleSystem>();
            foreach (var ps in allParticleSystems)
            {
                if (ps != null) ps.emission.enabled = true;
            }
        }

        public enum ParticleTypes { Empty }
    }
}