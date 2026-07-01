using UnityEngine;
using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using System.Collections.Generic;
#endif
namespace Magnetar_Client.Modules
{
    public class CleanWeather : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Clean Weather";
        public override string Description { get; set; } = "Clears fog/snowfall";
        public override string SearchHints { get; set; } = "cleanweather clearweather nofog nosnow weatherclear " +
            "weatherfix removefog removesnow clearfog clearsnow weathercontrol weatherclean skyclear fogremover " +
            "snowremover no-weather weather-clear clear-weather visibilityfix clearweather-mod weather-mod " +
            "weatherhack";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        // Mod Data

        public static CleanWeather instance;

        public BoolSetting ClearFog;
        public BoolSetting ClearSnowFall;

        public CleanWeather()
        {
            instance = this;

            CreateCategory("General");

            ClearFog = new BoolSetting("Clear Fog", true);
            ClearSnowFall = new BoolSetting("Clear SnowFall", true);

            AddSettings(ClearFog,ClearSnowFall);

            EndCategory();
        }

        // Mod Logic

        public override void OnEnable()
        {
            if (ClearFog.Value && FogMgr.Instance != null)
            {
                foreach (var block in FogMgr.Instance.fogList)
                {
                    var renderers = block.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = false;
                    }
                }
            }

            
        }

        public override void OnUpdateActive()
        {
            if (ClearSnowFall.Value && SnowMap.Instance != null)
            {
                var Partcles = SnowMap.Instance.GetComponentsInChildren<ParticleSystem>();
                foreach (var part in Partcles)
                {
                    if (part != null)
                    {
                        part.emission.enabled = false;
                    }
                }
            }
        }

        public override void OnDisable()
        {
            if (FogMgr.Instance != null)
                foreach (var block in FogMgr.Instance.fogList)
                {
                    var renderers = block.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = true;
                    }
                }
            if (SnowMap.Instance != null)
            {
                var Partcles = SnowMap.Instance.GetComponentsInChildren<ParticleSystem>();
                foreach (var part in Partcles)
                {
                    if (part != null && part.gameObject.name != "Snow2") part.emission.enabled = true;
                }
            }
        }


        [HarmonyPatch(typeof(Board))]
        public static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Start))]
            [HarmonyPostfix]
            public static void Start()
            {
                if (instance == null || !instance.Active) return;
                instance.OnEnable();
            }
        }
    }
}