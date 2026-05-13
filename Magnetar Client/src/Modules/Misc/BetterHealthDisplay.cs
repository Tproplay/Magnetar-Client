using HarmonyLib;
using Il2Cpp;
using static Magnetar_Client.Utils.Maths;
using static Magnetar_Client.Game.GameData;

namespace Magnetar_Client.Modules
{
    public class BetterHealthDisplay : Module
    {
        // Mod Info

        public override string Name { get; set; } = "Better Health Display";
        public override string Description { get; set; } = "Converts Long health texts to a clear shorter form";
        public override string SearchHints { get; set; } = "healthdisplay healthtext healthshow hpdisplay hptext betterhealth clearhealth" +
            " shorthealth healthform shorthp clearhp healthconverter texthp hpconverter shorttext healthui hpui hpvisual healthvisual " +
            "betterhp displayhealth healthshortener hp-display health-display texthp health-txt hptxt healthtxt";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        static BetterHealthDisplay instance;

        public BoolSetting ShowMaxHealth;

        public BetterHealthDisplay()
        {
            instance = this;

            ShowMaxHealth = new BoolSetting("Show Max Health", true);
            Settings.Add(ShowMaxHealth);
        }

        // Mod Logic

        static void UpdateTexts()
        {
            foreach (Plant plant in plantList)
            {
                plant.UpdateText();
            }

            foreach (Zombie zombie in zombieList)
            {
                zombie.UpdateHealthText();
            }
        }

        // Ensure the effect is seen immediately
        public override void OnEnable() { UpdateTexts(); }
        public override void OnDisable() { UpdateTexts(); }


        [HarmonyPatch(typeof(Plant))]
        public static class PlantTextPatch
        {
            [HarmonyPatch(nameof(Plant.UpdateText))]
            [HarmonyPostfix]
            public static void UpdateTextPostfix(Plant __instance)
            {
                if (!instance.Active || __instance == null) return;

                var textComponents = __instance.GetComponentsInChildren<Il2CppTMPro.TextMeshPro>();

                foreach (var textComp in textComponents)
                {
                    string rawHpString = __instance.thePlantHealth.ToString();

                    if (textComp.text == rawHpString || textComp.text.Contains("/"))
                    {
                        string formattedCurrent = (__instance.thePlantHealth).ToString();

                        if (instance.ShowMaxHealth.Value)
                        {
                            string formattedMax = FormatInternational(__instance.thePlantMaxHealth);
                            textComp.text = $"{formattedCurrent} / {formattedMax}";
                        }
                        else
                        {
                            textComp.text = formattedCurrent;
                        }
                    }
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(Zombie))]
        public static class ZombieTextPatch
        {
            [HarmonyLib.HarmonyPatch(nameof(Zombie.UpdateHealthText))]
            [HarmonyLib.HarmonyPostfix]
            public static void UpdateHealthTextPostfix(Zombie __instance)
            {
                if (instance == null || !instance.Active || __instance == null) return;

                if (__instance.healthText == null) return;

                // Grab the total health (Base Health + Armor 1 + Armor 2)
                // And Display only them
                int currentHp = __instance.CurrentAllHealth;
                int maxHp = __instance.TotalAllHealth;

                string formattedCurrent = FormatInternational(currentHp);
                string finalText = formattedCurrent;

                if (instance.ShowMaxHealth.Value)
                {
                    string formattedMax = FormatInternational(maxHp);
                    finalText = $"{formattedCurrent} / {formattedMax}";
                }

                __instance.healthText.text = finalText;

                if (__instance.healthTextShadow != null)
                {
                    __instance.healthTextShadow.text = finalText;
                }
            }
        }

    }
}
