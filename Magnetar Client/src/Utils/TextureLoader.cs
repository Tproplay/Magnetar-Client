using Il2Cpp;
using Il2CppSystem.IO;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils
{
    public static class TextureLoader
    {
        private static AssetBundle _dataBundle = null;
        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        private static Dictionary<string, Sprite> _spriteMemoryCache = new Dictionary<string, Sprite>();
        private static Dictionary<string, Texture2D> _rawTexMemoryCache = new Dictionary<string, Texture2D>();
        private static bool _hasScannedMemory = false;

        public static Dictionary<int, string> PlantTextureOverrides = new Dictionary<int, string>();
        public static Dictionary<int, string> ZombieTextureOverrides = new Dictionary<int, string>();

        private static float _lastBundleCheckTime = -10f;
        private static bool _bundleDiagnosticPrinted = false;

        public static void InitializeBundle()
        {
            if (_dataBundle != null) return;

            if (Time.realtimeSinceStartup - _lastBundleCheckTime < 2f) return;
            _lastBundleCheckTime = Time.realtimeSinceStartup;

            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles().ToArray();

            foreach (var bundle in loadedBundles)
            {
                if (bundle != null)
                {
                    if (!_bundleDiagnosticPrinted)
                    {
#if DEBUG
                        DebugLogger.Msg($"Game has active bundle: '{bundle.name}'");
#endif
                    }

                    string bName = bundle.name.ToLower();
                    if (bName.Contains("data") || bName.Contains("main") || bName.Contains("plant") || bName.Contains("fusion"))
                    {
                        _dataBundle = bundle;
#if DEBUG
                        DebugLogger.Msg($"Successfully hooked AssetBundle: '{bundle.name}'");
#endif
                        return;
                    }
                }
            }
            _bundleDiagnosticPrinted = true;

            string bundlePath = Path.Combine(Application.streamingAssetsPath, "data.Unity3d");
            if (!File.Exists(bundlePath)) bundlePath = Path.Combine(Application.dataPath, "data.Unity3d");

            if (File.Exists(bundlePath))
            {
                _dataBundle = AssetBundle.LoadFromFile(bundlePath);
#if DEBUG
                DebugLogger.Msg("Loaded AssetBundle from file disk.");
#endif
            }
        }

        private static void RefreshMemoryCache()
        {
            _spriteMemoryCache.Clear();
            _rawTexMemoryCache.Clear();

            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < allSprites.Count; i++)
            {
                Sprite s = allSprites[i];
                if (s != null && !string.IsNullOrEmpty(s.name))
                {
                    if (!_spriteMemoryCache.ContainsKey(s.name))
                        _spriteMemoryCache[s.name] = s;
                }
            }

            var allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
            for (int i = 0; i < allTextures.Count; i++)
            {
                Texture2D t = allTextures[i];
                if (t != null && !string.IsNullOrEmpty(t.name))
                {
                    if (!_rawTexMemoryCache.ContainsKey(t.name))
                        _rawTexMemoryCache[t.name] = t;
                }
            }
            _hasScannedMemory = true;
        }

        public static Texture2D GetTexture(string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;

            if (_textureCache.TryGetValue(textureName, out Texture2D cachedTex))
                return cachedTex;

            if (_dataBundle == null) InitializeBundle();

            bool isExplicitPath = textureName.Contains("/");

            // ====================================================================
            // STAGE 1: EXPLICIT PATH OVERRIDES
            // ====================================================================
            if (isExplicitPath)
            {
                string cleanPath = textureName.ToLower();
                int targetIndex = 0;
                bool useSubAsset = false;

                if (cleanPath.EndsWith("]"))
                {
                    int openBracket = cleanPath.LastIndexOf('[');
                    if (openBracket != -1)
                    {
                        string idxStr = cleanPath.Substring(openBracket + 1, cleanPath.Length - openBracket - 2);
                        if (int.TryParse(idxStr, out int parsedIdx))
                        {
                            targetIndex = parsedIdx;
                            useSubAsset = true;
                            cleanPath = cleanPath.Substring(0, openBracket);
                        }
                    }
                }

                string resPath = cleanPath.Replace(".png", "").Replace(".jpg", "");

                if (useSubAsset)
                {
                    var subAssets = Resources.LoadAll<Sprite>(resPath);
                    if (subAssets != null && subAssets.Length > 0)
                    {
                        int safeIndex = (targetIndex >= 0 && targetIndex < subAssets.Length) ? targetIndex : 0;
                        Texture2D isolatedTex = CreateReadableCroppedTexture(subAssets[safeIndex]);
                        if (isolatedTex != null)
                        {
                            _textureCache[textureName] = isolatedTex;
                            return isolatedTex;
                        }
                    }
                }
                else
                {
                    Sprite resSprite = Resources.Load<Sprite>(resPath);
                    if (resSprite != null)
                    {
                        Texture2D isolatedTex = CreateReadableCroppedTexture(resSprite);
                        if (isolatedTex != null)
                        {
                            _textureCache[textureName] = isolatedTex;
                            return isolatedTex;
                        }
                    }
                }

                if (_dataBundle != null)
                {
                    string exactInternalPath = null;
                    string[] allBundlePaths = _dataBundle.GetAllAssetNames();

                    foreach (string bPath in allBundlePaths)
                    {
                        if (bPath.Contains(cleanPath))
                        {
                            exactInternalPath = bPath;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(exactInternalPath))
                    {
                        var subSprites = _dataBundle.LoadAssetWithSubAssets<Sprite>(exactInternalPath);
                        if (subSprites != null && subSprites.Length > 0)
                        {
                            int safeIndex = (useSubAsset && targetIndex >= 0 && targetIndex < subSprites.Length) ? targetIndex : 0;
                            Texture2D isolatedTex = CreateReadableCroppedTexture(subSprites[safeIndex]);
                            if (isolatedTex != null)
                            {
                                _textureCache[textureName] = isolatedTex;
                                return isolatedTex;
                            }
                        }

                        Texture2D rawTex = _dataBundle.LoadAsset<Texture2D>(exactInternalPath);
                        if (rawTex != null)
                        {
                            _textureCache[textureName] = rawTex;
                            return rawTex;
                        }
                    }
                }

                _textureCache[textureName] = null;
                return null;
            }

            // ====================================================================
            // STAGE 2: STANDARD SHORT NAME FALLBACKS
            // ====================================================================
            if (_dataBundle != null)
            {
                Sprite bundleSprite = _dataBundle.LoadAsset<Sprite>(textureName);
                if (bundleSprite != null && bundleSprite.name == textureName)
                {
                    Texture2D isolatedTex = CreateReadableCroppedTexture(bundleSprite);
                    if (isolatedTex != null)
                    {
                        _textureCache[textureName] = isolatedTex;
                        return isolatedTex;
                    }
                }
            }

            if (!_hasScannedMemory) RefreshMemoryCache();

            if (_spriteMemoryCache.TryGetValue(textureName, out Sprite sprite))
            {
                if (sprite != null)
                {
                    Texture2D isolatedTex = CreateReadableCroppedTexture(sprite);
                    if (isolatedTex != null)
                    {
                        _textureCache[textureName] = isolatedTex;
                        return isolatedTex;
                    }
                }
                else
                {
                    _spriteMemoryCache.Remove(textureName);
                }
            }

            if (_rawTexMemoryCache.TryGetValue(textureName, out Texture2D tex))
            {
                if (tex != null)
                {
                    _textureCache[textureName] = tex;
                    return tex;
                }
                else
                {
                    _rawTexMemoryCache.Remove(textureName);
                }
            }

            _textureCache[textureName] = null;
            return null;
        }

        private static Texture2D CreateReadableCroppedTexture(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return null;

            Texture2D sourceTex = sprite.texture;
            Rect textureRect = sprite.textureRect;

            int width = (int)textureRect.width;
            int height = (int)textureRect.height;

            RenderTexture tempRT = RenderTexture.GetTemporary(
                sourceTex.width,
                sourceTex.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default
            );

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = tempRT;

            GL.Clear(false, true, new Color(1f, 1f, 1f, 0f));

            Graphics.Blit(sourceTex, tempRT);

            Texture2D readableCopy = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
            readableCopy.ReadPixels(new Rect(0, 0, sourceTex.width, sourceTex.height), 0, 0);
            readableCopy.Apply();

            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(tempRT);

            Texture2D readableCroppedTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] pixels = readableCopy.GetPixels((int)textureRect.x, (int)textureRect.y, width, height);

            readableCroppedTex.SetPixels(pixels);
            readableCroppedTex.Apply();

            Object.Destroy(readableCopy);

            return readableCroppedTex;
        }

        public static Texture2D GetPlantTexture(int plantId)
        {
            if (PlantTextureOverrides.TryGetValue(plantId, out string texturePath))
            {
                Texture2D overrideTex = GetTexture(texturePath);
                if (overrideTex != null) return overrideTex;
            }

            string rawName = ((PlantType)plantId).ToString();
            string enumNameLower = rawName.ToLower();
            string preferredPath = $"plants/{enumNameLower}/{enumNameLower}";

            Texture2D tex = GetTexture(preferredPath);
            string successfulPath = preferredPath;

            if (tex == null)
            {
                tex = GetTexture(rawName);
                successfulPath = rawName;
            }
#if DEBUG
            if (tex == null)
            {
                if (!PlantTextureOverrides.ContainsKey(plantId))
                {
                    PlantTextureOverrides[plantId] = preferredPath;
                    SaveLoad.Save();
                }
            }
            else
            {
                if (!PlantTextureOverrides.ContainsKey(plantId))
                {
                    PlantTextureOverrides[plantId] = successfulPath;
                    SaveLoad.Save();
                }
            }
#endif
            return tex;
        }

        public static Texture2D GetZombieTexture(int zombieId)
        {
            if (ZombieTextureOverrides.TryGetValue(zombieId, out string texturePath))
            {
                Texture2D overrideTex = GetTexture(texturePath);
                if (overrideTex != null) return overrideTex;
            }

            string rawName = ((ZombieType)zombieId).ToString();
            string enumNameLower = rawName.ToLower();
            string preferredPath = $"zombies/{enumNameLower}/{enumNameLower}";

            Texture2D tex = GetTexture(preferredPath);
            string successfulPath = preferredPath;

            if (tex == null)
            {
                tex = GetTexture(rawName);
                successfulPath = rawName;
            }
#if DEBUG
            if (tex == null)
            {
                if (!ZombieTextureOverrides.ContainsKey(zombieId))
                {
                    ZombieTextureOverrides[zombieId] = preferredPath;
                    SaveLoad.Save();
                }
            }
            else
            {
                if (!ZombieTextureOverrides.ContainsKey(zombieId))
                {
                    ZombieTextureOverrides[zombieId] = successfulPath;
                    SaveLoad.Save();
                }
            }
#endif
            return tex;
        }
    }

    public static class LoadFont
    {
        private static Font customWineFont;

        public static void Init()
        {
            if (GUI.skin != null && GUI.skin.font == customWineFont) return;

            string bundlePath = Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Data", "magnetar_ui");

            if (File.Exists(bundlePath))
            {
                AssetBundle fontBundle = AssetBundle.LoadFromFile(bundlePath);

                if (fontBundle != null)
                {
                    customWineFont = fontBundle.LoadAsset<Font>("Magnetar_font");

                    if (customWineFont != null)
                    {
                        GUI.skin.font = customWineFont;
                        GUI.skin.box.font = customWineFont;
                        GUI.skin.label.font = customWineFont;
                        GUI.skin.button.font = customWineFont;
                        GUI.skin.textField.font = customWineFont;
                        GUI.skin.textArea.font = customWineFont;
                        GUI.skin.toggle.font = customWineFont;
                        GUI.skin.window.font = customWineFont;

                        DebugLogger.Msg("[Texture Loader] Successfully loaded and applied font!");
                    }
                    else
                    {
                        DebugLogger.Error("[Texture Loader] Found bundle, but 'Magnetar_font' asset was missing inside it.");
                    }

                    fontBundle.Unload(false);
                }
            }
            else
            {
                DebugLogger.Warning("[Texture Loader] Font AssetBundle not found!");
            }
        }
    }
}