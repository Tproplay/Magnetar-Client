using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class ZombieWaveHealth : HudElement
{
    public ZombieWaveHealth() : base("Current Zombie Wave Health", HudElement.NewRect(235))
    { UpdateInterval = 0.2f; }

    string displayText = "Wave Health: Na";
    
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            displayText = 
                $"Wave Health: {FormatInternational(GameData.BoardInstance.zombieCurrentWaveHealth)}/" +
                $"{FormatInternational(GameData.BoardInstance.zombieSpawnHealth)}";
        }
        else
        {
            displayText = $"Wave Health: Na";
        }
        
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }

}
