using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Utils.Maths;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.HUDElements
{
    public class DamageStatsPlant : HudElement
    {
        public DamageStatsPlant() : base("Total Damage by Plants (Zombie HP Loss)", HudElement.NewRect(100))
        { }

        
        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Plant Damage: {FormatInternational(TotalDamagedRecievedByZombies)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }


    public class AverageDamageStatsPlant : HudElement
    {
        public AverageDamageStatsPlant() : base("Average Damage by Plants (Zombie HP Loss)", HudElement.NewRect(100))
        { }

        private List<long> damageHistory = new List<long>();

        private float timer = 0;
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;

            timer += Time.deltaTime;

            if (timer >= 0.25f)
            {
                timer = 0f;

                damageHistory.Add(TotalDamagedRecievedByZombies);

                // Keep only the last 10 snapshots (covering 2.5 second of gameplay)
                if (damageHistory.Count > 10)
                {
                    damageHistory.RemoveAt(0);
                }
            }
        }

        long Dps = 0;

        protected override void DrawContent(float width, float height)
        {

            if (damageHistory.Count>=10)
                Dps = damageHistory[9] - damageHistory[0];
            if (Dps < 0) Dps = 0;

            string displayText = $"Plant DPS: {FormatInternational(Dps)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
