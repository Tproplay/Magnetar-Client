using HarmonyLib;
using Magnetar_Client.Game;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;
using System.Linq;
using System.Collections.Generic;
using System;




#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client.Modules
{
    public class BetterHealthDisplay : Module
    {
        // Mod Info

        public override string Name { get; set; } = "Better Health Display";
        public override string Description { get; set; } = "Converts long health texts to a much more clear shorter form.";
        public override string SearchHints { get; set; } = "healthdisplay healthtext healthshow hpdisplay hptext betterhealth clearhealth" +
            " shorthealth healthform shorthp clearhp healthconverter texthp hpconverter shorttext healthui hpui hpvisual healthvisual " +
            "betterhp displayhealth healthshortener hp-display health-display texthp health-txt hptxt healthtxt";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        public override bool Active { get; set; } = true; // On by default

        // Mod Data
        public static BetterHealthDisplay instance;

        public BoolSetting ShowMaxHealth;
        public BoolSetting AutoEnable_ShowHp_Plant;
        public BoolSetting AutoEnable_ShowHp_Zombie;
        public MultiSelectSetting SelectedPlants;
        public MultiSelectSetting SelectedZombies;
        public BoolSetting SumHpSetting;

        public BoolSetting ShowControlledPlant;
        
        public BetterHealthDisplay()
        {
            instance = this;

            CreateCategory("General");

            SumHpSetting = new BoolSetting("Combine HP and Shield", true)
            {
                OnValueChanged = UpdateTexts
            };
            

            AddSettings(SumHpSetting);

            EndCategory();

            CreateCategory("Plants");

            ShowMaxHealth = new BoolSetting("Show Max Health", false)
            {
                OnValueChanged = UpdateTexts
            };

            SelectedPlants = new MultiSelectSetting("Whitelist Plants", typeof(PlantType))
            {
                CustomNames = TranslatedNames(typeof(PlantType)),
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,3000
                },
            };
            foreach (var item in SelectedPlants.Options.Keys)
            {
                SelectedPlants.Select(item);
            }

            AddSettings(SelectedPlants, ShowMaxHealth);

            EndCategory();

            CreateCategory("Zombies");

            SelectedZombies = new MultiSelectSetting("Whitelist Zombies", typeof(ZombieType))
            {
                CustomNames = TranslatedNames(typeof(ZombieType)),
                Blacklist = new HashSet<int> {
                (int)ZombieType.Nothing
                }
            };
            foreach (var item in SelectedZombies.Options.Keys)
            {
                SelectedZombies.Select(item);
            }

            AddSettings(SelectedZombies);

            EndCategory();

            CreateCategory("Extra");

            AutoEnable_ShowHp_Plant = new BoolSetting("Auto Enable Plant Hp", false);
            AutoEnable_ShowHp_Zombie = new BoolSetting("Auto Enable Zombie Hp", false);

            ShowControlledPlant = new BoolSetting("Show Star icon on Controlled Plant", true)
            {
                OnValueChanged = UpdateTexts
            };
            AddSettings(AutoEnable_ShowHp_Plant, AutoEnable_ShowHp_Zombie, ShowControlledPlant);
            EndCategory();
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

        static void UpdateTexts<T>(T input)
        {
            UpdateTexts(); 
        }

        // Ensure the effect is seen immediately

        public override void OnEnable() { UpdateTexts(); }
        public override void OnDisable() { UpdateTexts(); }


        // Patches

        [HarmonyPatch(typeof(Plant))]
        public static class PlantTextPatch
        {
            [HarmonyPatch(nameof(Plant.UpdateText))]
            [HarmonyPostfix]
            public static void UpdateTextPostfix(Plant __instance)
            {
                if (instance == null || !instance.Active) return;
                if (__instance.healthSlider == null) return;

                var textComponents = __instance.healthSlider.GetComponentsInChildren<TMP_Text>(true);

                string rawHpString = __instance.thePlantHealth.ToString();

                if (!instance.SelectedPlants.IsSelected((int)__instance.thePlantType))
                {
                    foreach (var textComp in textComponents)
                    {
                        textComp.text = String.Empty;
                    }
                    return;
                }

                foreach (var textComp in textComponents)
                {
                    string rawText = textComp.text ?? string.Empty;
                    string cleanText = System.Text.RegularExpressions.Regex.Replace(rawText, "<.*?>", string.Empty);

                    if (cleanText == rawHpString || cleanText.Contains("/") || cleanText.Contains("+"))
                    {
                        string leftSide = cleanText.Split('/')[0].Trim();

                        string[] plusParts = leftSide.Split('+');
                        string formattedCurrent;

                        if (instance.SumHpSetting.Value && plusParts.Length > 1)
                        {
                            int sum = 0;
                            foreach (var part in plusParts)
                            {
                                if (int.TryParse(part.Trim(), out int val))
                                {
                                    sum += val;
                                }
                            }
                            formattedCurrent = FormatInternational(sum);
                        }
                        else
                        {
                            var formattedList = new System.Collections.Generic.List<string>();
                            foreach (var part in plusParts)
                            {
                                if (int.TryParse(part.Trim(), out int val))
                                {
                                    formattedList.Add(FormatInternational(val));
                                }
                                else
                                {
                                    formattedList.Add(part.Trim());
                                }
                            }
                            formattedCurrent = string.Join("+", formattedList);
                        }

                        string finalText = formattedCurrent;

                        if (instance.ShowMaxHealth.Value)
                        {
                            string formattedMax = FormatInternational(__instance.thePlantMaxHealth);
                            finalText = $"{formattedCurrent} / {formattedMax}";
                        }

                        if (instance.ShowControlledPlant.Value && board.controledPlant == __instance)
                        {
                            finalText = $"<size=200%><color=yellow>★</color>\n{finalText}</size>";
                        }
                        
                        textComp.text = finalText;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Zombie))]
        public static class ZombieTextPatch
        {
            [HarmonyPatch(nameof(Zombie.UpdateHealthText))]
            [HarmonyPostfix]
            public static void UpdateHealthTextPostfix(Zombie __instance)
            {
                if (instance == null || !instance.Active) return;

                if (__instance.healthText == null) return;

                if (!instance.SelectedZombies.IsSelected((int)__instance.theZombieType))
                {
                    __instance.healthText.text = string.Empty;
                    return;
                }

                // Grab the total health (Base Health + Armor 1 + Armor 2)
                // And Display only them
                long currentHp = __instance.CurrentAllHealth;
                long maxHp = __instance.TotalAllHealth;

                string formattedCurrent = FormatInternational(currentHp);
                string finalText = formattedCurrent;

                if (instance.ShowMaxHealth.Value)
                {
                    string formattedMax = FormatInternational(maxHp);
                    finalText = $"{formattedCurrent} / {formattedMax}";
                }
                
                __instance.healthText.text = finalText;
            }
        }


        [HarmonyPatch(typeof(Board))]
        public class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Awake))]
            [HarmonyPostfix]
            static void AwakePostFix(Board __instance)
            {
                if (instance == null || !instance.Active) return;
                if (instance.AutoEnable_ShowHp_Plant.Value) __instance.showPlantHealth = 1;
                if (instance.AutoEnable_ShowHp_Zombie.Value) __instance.showZombieHealth = true;
            }
        }
    }
}
