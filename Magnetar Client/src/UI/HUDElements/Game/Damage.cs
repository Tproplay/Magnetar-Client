using Il2Cpp;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.UI.HUDElements
{
    public class DamageStatsPlant : HudElement
    {
        public DamageStatsPlant() : base("Total Damage by Plants", HudElement.NewRect(200))
        { }

        
        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Plant Damage: {FormatInternational(ZombieDamage)}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }


    public class AverageDamageStatsPlant : HudElement
    {
        public AverageDamageStatsPlant() : base("Average Damage by Plants", HudElement.NewRect(200))
        { }

        private List<long> damageHistory = new List<long>();

        private float timer = 0;
        public override void OnUpdateActive()
        {
            if (Board.Instance == null) return;

            timer += Time.deltaTime;

            if (timer >= 0.25f)
            {
                timer = 0f;

                damageHistory.Add(ZombieDamage);

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

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
