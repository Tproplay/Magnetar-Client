using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;
using UnityEngine;
using System.Linq;
using Magnetar_Client.Game;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements
{
    public class NumberOfZombies : HudElement
    {
        public NumberOfZombies() : base("Zombies On Lawn", HudElement.NewRect(100))
        { UpdateInterval = 0.2f; }
        string displayText = "Zombies On Lawn: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Zombies On Lawn: {zombieList.Count(z => !z.isMindControlled)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfHypnotizedZombies : HudElement
    {
        public NumberOfHypnotizedZombies() : base("Hypnotized Zombies On Lawn", HudElement.NewRect(100))
        { UpdateInterval = 0.2f; }

        string displayText = "Hypno Zombies On Lawn: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Hypno Zombies On Lawn: {zombieList.Count(z => z.isMindControlled)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfZombiesSpawned : HudElement
    {
        public NumberOfZombiesSpawned() : base("Zombies Spawned", HudElement.NewRect(100))
        { UpdateInterval = 0.2f; }

        string displayText = "Zombies Spawned: Na";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Zombies Spawned: {AppData.board.boardStatistics.zombiesKilled + GameData.zombieList.Count}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfHypnotizedZombiesSpawned : HudElement
    {
        public NumberOfHypnotizedZombiesSpawned() : base("Hypnotized Zombies Spawned", HudElement.NewRect(100))
        { UpdateInterval = 0.2f; }

        string displayText = "Hypno Zombies Spawned: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Hypno Zombies Spawned: {GameData.Hypno_Zombies_Spawned}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfZombiesKilled : HudElement
    {
        public NumberOfZombiesKilled() : base("Zombies Killed", HudElement.NewRect(220))
        { UpdateInterval = 0.2f; }

        string displayText = "Zombies Killed: Na";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Zombies Killed: {AppData.board.boardStatistics.zombiesKilled}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfHypnotizedZombiesKilled : HudElement
    {
        public NumberOfHypnotizedZombiesKilled() : base("Hypnotized Zombies Killed", HudElement.NewRect(220))
        { UpdateInterval = 0.2f; }

        string displayText = "Hypno Zombies Killed: Na";

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Hypno Zombies Killed: {GameData.Hypno_Zombies_Killed}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

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
            if (!AppData.BoardInstanceIsNull)
            {
                displayText = 
                    $"Wave Health: {FormatInternational(AppData.board.zombieCurrentWaveHealth)}/" +
                    $"{FormatInternational(AppData.board.zombieSpawnHealth)}";
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

    public class TotalZombieHealth : HudElement
    {
        public TotalZombieHealth() : base("Total Zombies Health", HudElement.NewRect(235))
        { UpdateInterval = 0.2f; }

        string displayText = "Total Zombies Health: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        long GetZombieHealth()
        {
            long health = 0;
            foreach (Zombie zombie in zombieList)
            {
                if (zombie.isMindControlled) continue;
                health += zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth;

            }
            return health > 0 ? health : 0;
        }

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;

            displayText = $"Total Zombies Health: {FormatInternational(GetZombieHealth())}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }

    }

    public class TotalHypnotizedZombieHealth : HudElement
    {
        public TotalHypnotizedZombieHealth() : base("Hypnotized Zombies Health", HudElement.NewRect(235))
        { UpdateInterval = 0.2f; }

        string displayText = "Hypno Zombies Health: Na";

        protected override void DrawContent(float width, float height)
        {
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

        public override void OnUpdateActive()
        {
            if (AppData.BoardInstanceIsNull) return;
            displayText = $"Hypno Zombies Health: {FormatInternational(GetZombieHealth())}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class CurrentWave : HudElement
    {
        public CurrentWave() : base("Current Wave", HudElement.NewRect(235))
        { UpdateInterval = 1f; }

        string displayText = "Wave: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (!AppData.BoardInstanceIsNull)
            {
                displayText = $"Wave: {AppData.board.theWave + 1}/{AppData.board.theMaxWave + 1}";
            }
            else
            {
                displayText = "Wave: Na";
            }

            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }

        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }

    }


}
