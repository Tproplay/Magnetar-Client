using UnityEngine;
using System.Collections.Generic;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.NEF.Data.NEFRecipes;
using Magnetar_Client.UI;
using Magnetar_Client.Core;

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

        Texture2D finalTex = entity.IsZombie
            ? ResourceManager.GetZombieTexture(entity.Id)
            : ResourceManager.GetPlantTexture(entity.Id);

        if (finalTex == null) return null;

        GUIStyle newStyle = new();
        newStyle.normal.background = finalTex;
        cachedEntityStyles[entity.Id] = newStyle;

        return newStyle;
    }
}