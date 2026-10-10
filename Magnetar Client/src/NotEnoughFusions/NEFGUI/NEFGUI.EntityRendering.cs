using UnityEngine;
using System.Collections.Generic;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using static Magnetar_Client.NEF.Data.NEFRecipes;
using Magnetar_Client.UI;
using Magnetar_Client.Core;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF;

public static partial class NEFGUI
{
    private static readonly Dictionary<int, GUIStyle> cachedEntityStyles = new();

    private static void DrawSquareNodeBox(Rect rect, RecipeEntity entity, float scale)
    {
        ThemeManager.NEFNodeStyle.fontSize = Mathf.Max(1, (int)(GUIManager.S(8f) * scale));
        string displayName = NEFData.GetEntityName(entity);
        GUI.Box(rect, displayName, ThemeManager.NEFNodeStyle);

        GUIStyle imgStyle = GetEntityStyle(entity);

        if (imgStyle != null && imgStyle.normal.background != null)
        {
            Texture2D tex = imgStyle.normal.background;

            float pad = GUIManager.S(10f) * scale;
            float bottomTextSpace = GUIManager.S(25f) * scale;

            float availWidth = rect.width - (pad * 2f);
            float availHeight = rect.height - pad - bottomTextSpace;

            float texAspect = (float)tex.width / Mathf.Max(1, tex.height);
            float availAspect = availWidth / availHeight;

            float drawWidth = availWidth;
            float drawHeight = availHeight;

            if (texAspect > availAspect) drawHeight = availWidth / texAspect;
            else drawWidth = availHeight * texAspect;

            float centerX = rect.x + pad + (availWidth / 2f);
            float centerY = rect.y + pad + (availHeight / 2f);

            Rect imageRect = new(centerX - (drawWidth / 2f), centerY - (drawHeight / 2f), drawWidth, drawHeight);
            GUI.Box(imageRect, GUIContent.none, imgStyle);
        }
    }

    private static GUIStyle GetEntityStyle(RecipeEntity entity)
    {
        if (cachedEntityStyles.TryGetValue(entity.Id, out GUIStyle style))
        {
            return style;
        }

        Texture2D finalTex = null;

        if (NEFData.LegacyLoadEntities.Contains(entity.Id) || entity.Id >= 3000)
        {
            finalTex = entity.IsZombie
                ? TextureLoader.GetZombieTexture(entity.Id)
                : TextureLoader.GetPlantTexture(entity.Id);
        }

        if (finalTex == null)
        {
            Sprite sprite = GetEntitySprite(entity);
            if (sprite != null && sprite.texture != null)
            {
                if (sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height)
                {
                    finalTex = sprite.texture;
                }
                else
                {
                    RenderTexture tmp = RenderTexture.GetTemporary(
                        sprite.texture.width,
                        sprite.texture.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

                    Graphics.Blit(sprite.texture, tmp);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = tmp;

                    finalTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
                    finalTex.ReadPixels(new Rect(sprite.rect.x, sprite.rect.y, sprite.rect.width, sprite.rect.height), 0, 0);
                    finalTex.Apply();

                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(tmp);
                }
            }
        }

        if (finalTex == null) return null;

        GUIStyle newStyle = new();
        newStyle.normal.background = finalTex;
        cachedEntityStyles[entity.Id] = newStyle;

        return newStyle;
    }

    private static Sprite GetEntitySprite(RecipeEntity entity)
    {
        if (GameAPP.resourcesManager == null) return null;

        if (entity.IsZombie)
        {
            ZombieType zType = (ZombieType)entity.Id;
            if (GameAPP.resourcesManager.zombieSprites.ContainsKey(zType))
            {
                return GameAPP.resourcesManager.zombieSprites[zType];
            }
        }
        else
        {
            PlantType pType = (PlantType)entity.Id;
            if (GameAPP.resourcesManager.plantPreviews.ContainsKey(pType))
            {
                GameObject previewObj = GameAPP.resourcesManager.plantPreviews[pType];
                if (previewObj != null)
                {
                    SpriteRenderer sr = previewObj.GetComponent<SpriteRenderer>();
                    if (sr != null) return sr.sprite;

                    UnityEngine.UI.Image img = previewObj.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) return img.sprite;
                }
            }
        }
        return null;
    }
}