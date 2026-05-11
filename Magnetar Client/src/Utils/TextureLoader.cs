using Il2CppSystem.IO;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Magnetar_Client.Utils
{
    public static class TextureLoader
    {
        private static AssetBundle _dataBundle = null;
        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        private static bool _initAttempted = false;

        public static void InitializeBundle()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles().ToArray();

            foreach (var bundle in loadedBundles)
            {
                if (bundle != null && bundle.name.ToLower().Contains("data"))
                {
                    _dataBundle = bundle;
                    MelonLogger.Msg("Intercepted existing 'data' bundle from game memory!");
                    return;
                }
            }

            string bundlePath = System.IO.Path.Combine(Application.streamingAssetsPath, "data.Unity3d");

            if (!System.IO.File.Exists(bundlePath))
                bundlePath = System.IO.Path.Combine(Application.dataPath, "data.Unity3d");

            if (!System.IO.File.Exists(bundlePath))
                { MelonLogger.Warning($"Could not find data.Unity3d at {bundlePath}"); return; }

            try
            {
                _dataBundle = AssetBundle.LoadFromFile(bundlePath);

                if (_dataBundle == null)
                    MelonLogger.Error("LoadFromFile returned null.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to load bundle: {ex.Message}");
            }
                
        }

        public static Texture2D GetTexture(string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;

            // 1. Check Cache
            if (_textureCache.TryGetValue(textureName, out Texture2D cachedTex))
                return cachedTex;

            // 2. Search Active Memory for the Sprite
            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var sprite in allSprites)
            {
                if (sprite != null && sprite.name.Equals(textureName, System.StringComparison.OrdinalIgnoreCase))
                {
                    Texture2D tex = sprite.texture;
                    _textureCache[textureName] = tex; // Cache it so we never search memory for this one again
                    return tex;
                }
            }

            // 3. Fallback: Search for raw Texture2D just in case it's not a Sprite
            var allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
            foreach (var tex in allTextures)
            {
                if (tex != null && tex.name.Equals(textureName, System.StringComparison.OrdinalIgnoreCase))
                {
                    _textureCache[textureName] = tex;
                    return tex;
                }
            }

            // 4. Not found in memory. Cache null to prevent lag spikes from searching every frame.
            MelonLogger.Warning($"Image '{textureName}' could not be found in active memory.");
            _textureCache[textureName] = null;
            return null;
        }
    }

    
}