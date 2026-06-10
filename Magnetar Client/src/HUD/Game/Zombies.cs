using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;
using UnityEngine;
using Il2Cpp;
using System.Linq;
namespace Magnetar_Client.HUDElements
{
    public class NumberOfZombies : HudElement
    {
        public NumberOfZombies() : base("Zombies On Lawn", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Zombies On Lawn: {zombieList.Count(z => !z.isMindControlled)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfHypnotizedZombies : HudElement
    {
        public NumberOfHypnotizedZombies() : base("Hypnotized Zombies On Lawn", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Hypno Zombies On Lawn: {zombieList.Count(z => z.isMindControlled)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfZombiesSpawned : HudElement
    {
        public NumberOfZombiesSpawned() : base("Zombies Spawned", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Zombies Spawned: {TotalNumberOfZombiesSpawed}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfHypnotizedZombiesSpawned : HudElement
    {
        public NumberOfHypnotizedZombiesSpawned() : base("Hypnotized Zombies Spawned", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Hypno Zombies Spawned: {TotalNumberOfHypnotizedZombiesSpawed}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfZombiesKilled : HudElement
    {
        public NumberOfZombiesKilled() : base("Zombies Killed", HudElement.NewRect(220))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Zombies Killed: {TotalNumberOfZombiesKilled}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfHypnotizedZombiesKilled : HudElement
    {
        public NumberOfHypnotizedZombiesKilled() : base("Hypnotized Zombies Killed", HudElement.NewRect(220))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Hypno Zombies Killed: {TotalNumberOfHypnotizedZombiesKilled}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class TotalZombieHealth : HudElement
    {
        public TotalZombieHealth() : base("Zombie Wave Health", HudElement.NewRect(235))
        { }

        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Wave Health: {FormatInternational(GetZombieHealth())}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }


        long GetZombieHealth()
        {
            long health = 0;
            foreach(Zombie zombie in zombieList)
            {
                if (zombie.isMindControlled) continue;
                health += zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth;
                
            }
            return health>0?health:0;
        }
    }

    public class TotalHypnotizedZombieHealth : HudElement
    {
        public TotalHypnotizedZombieHealth() : base("Hypnotized Zombies Health", HudElement.NewRect(235))
        { }

        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Hypno Zombies Health: {FormatInternational(GetZombieHealth())}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }


        long GetZombieHealth()
        {
            long health = 0;
            foreach (Zombie zombie in zombieList)
            {
                if (!zombie.isMindControlled) continue;
                health += zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth;

            }
            return health > 0 ? health : 0;
        }
    }
}
