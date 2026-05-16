using HarmonyLib;
using Il2Cpp;
using System.Collections.Generic;
using Magnetar_Client.Utils;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class CustomCDGlove : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Glove CD";
        public override string Description { get; set; } = "Set Custom Glove Cooldown.";
        public override string SearchHints { get; set; } = "noglovecd glovecooldown customglovecd " +
            "glovecd zeroglovecd noglovecoolndown glovecooldownreset fastglove instantglove glovebuff " +
            "glovecdmod glovecdchanger glovecdremover glovecolldown glovecooldon glovecooldwn glovcd " +
            "glovecooldoun glovecdtimer glovecdreducion glovecooldownreduction glovefast gloveready gloveunlimited " +
            "gloveinfinite gloveinterval gloveperiod glovespeed glovespam glovefrequency";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data
        public static CustomCDGlove instance;

        public float CustomCD = 0;
        public FloatSetting CustomCDSetting;

        private static float originalCD = -1f;

        public bool preserveOriginal = true;
        public BoolSetting preserveOriginalSetting;

        public bool resetCDonEnable = false;
        public BoolSetting resetCDonEnableSetting;

        public override bool Active { get; set; } = false;

        public CustomCDGlove()
        {
            CustomCDSetting = new FloatSetting("Custom Glove Cooldown", 0, 999, CustomCD);
            Settings.Add(CustomCDSetting);
            CustomCD = CustomCDSetting.Value;

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginal);
            Settings.Add(preserveOriginalSetting);
            preserveOriginal = preserveOriginalSetting.Value;

            resetCDonEnableSetting = new BoolSetting("Reset CD on Enable", resetCDonEnable);
            Settings.Add(resetCDonEnableSetting);
            resetCDonEnable = resetCDonEnableSetting.Value;
        }

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (Glove.Instance == null) return;
            if (originalCD < 0)
            {
                originalCD = Glove.Instance.coolSpeed;
            }
            Glove.Instance.fullCD = CustomCDSetting.Value;
        }

        public override void OnDisable()
        {
            originalCD = -1f;
            if (Glove.Instance == null) return;
            if (originalCD >= 0 && preserveOriginal)
            {
                Glove.Instance.fullCD = originalCD;
            }
        }

        public override void OnEnable()
        {
            if (Glove.Instance == null) return;
            if (resetCDonEnableSetting.Value)
            {
                Glove.Instance.CD = Glove.Instance.fullCD;
            }
        }
    }

    public class CustomCDHammer : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Hammer CD";
        public override string Description { get; set; } = "Set Custom Hammer Cooldown.";
        public override string SearchHints { get; set; } = "nohammercd hammercooldown customhammercd hammercd" +
            " zerohammercd nohammercoolndown hammercooldownreset fasthammer instanthammer hammerbuff hammercdmod " +
            "hammercdchanger hammercdremover hammercolldown hammercooldon hammercooldwn hamrcd hammercooldoun " +
            "hammercdtimer hammercdreducion hammercooldownreduction hammerfast hammerready hammerunlimited hammerinfinite " +
            "hammerinterval hammerperiod hammerspeed hammerspam hammerfrequency";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static CustomCDHammer instance;

        public float CustomCD = 0;
        public FloatSetting CustomCDSetting;

        private static float originalCD = -1f;

        public bool preserveOriginal = true;
        public BoolSetting preserveOriginalSetting;

        public bool resetCDonEnable = false;
        public BoolSetting resetCDonEnableSetting;

        public override bool Active { get; set; } = false;

        public CustomCDHammer()
        {
            instance = this;

            CustomCDSetting = new FloatSetting("Custom Hammer Cooldown", 0, 999, CustomCD);
            Settings.Add(CustomCDSetting);
            CustomCD = CustomCDSetting.Value;

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginal);
            Settings.Add(preserveOriginalSetting);
            preserveOriginal = preserveOriginalSetting.Value;

            resetCDonEnableSetting = new BoolSetting("Reset CD on Enable", resetCDonEnable);
            Settings.Add(resetCDonEnableSetting);
            resetCDonEnable = resetCDonEnableSetting.Value;
        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (HammerMgr.Instance == null)
            {
                originalCD = -1f;
                return;
            }

            // Save Original CD
            if (originalCD == -1f) originalCD = HammerMgr.Instance.fullCD;

            // BugFix: If the player has a higher CD than the one we set, we set it to our custom CD so
            // it doesn't take longer than intended to use the hammer again. This can happen if the player
            // has a CD increasing item and they enable this mod while the CD is still active.
            if (HammerMgr.Instance.CD > CustomCDSetting.Value)
            {
                HammerMgr.Instance.CD = CustomCDSetting.Value;
                HammerMgr.Instance.CDUpdate();
            }
            HammerMgr.Instance.fullCD = CustomCDSetting.Value;


        }

        public override void OnDisable()
        {

            if (HammerMgr.Instance == null) return;

            if (originalCD >= 0 && preserveOriginalSetting.Value)
            {
                HammerMgr.Instance.fullCD = originalCD;
            }
            originalCD = -1f;
        }

        public override void OnEnable()
        {
            if (HammerMgr.Instance == null) return;
            if (resetCDonEnableSetting.Value)
            {
                HammerMgr.Instance.CD = CustomCDSetting.Value;
            }
        }
    }

    public class CustomCDCards : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Cards CD";
        public override string Description { get; set; } = "Set Custom Cards Cooldown.";
        public override string SearchHints { get; set; } = "nocardscd cardscooldown customcardscd cardscd " +
            "zerocardscd nocardcoolndown cardcooldownreset fastcards instantcards cardbuff cardcdmod cardcdchanger " +
            "cardcdremover cardcolldown cardcooldon cardcooldwn cardcd cardcooldoun cardcdtimer cardcdreducion " +
            "cardcooldownreduction cardfast cardready cardunlimited cardinfinite cardinterval cardperiod cardspeed " +
            "cardspam cardfrequency";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static CustomCDCards instance;

        public float customCD = 100;
        public FloatSetting CustomCDSetting;

        public MultiSelectSetting selectedSeeds;
        public MultiSelectSetting selectedSeeds_dup;

        private Dictionary<int,string> plantNameOverriden = new Dictionary<int, string>();
        public CustomCDCards()
        {
            instance = this;

            plantNameOverriden = Translator.TranslateEnum(typeof(PlantType));

            foreach (var name in plantNameOverriden)
            {
                plantNameOverriden[name.Key] = $"{plantNameOverriden[name.Key]} ({name.Key})";
            }

            selectedSeeds = new MultiSelectSetting(
                "Cards", typeof(PlantType))
            {
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = plantNameOverriden
            };

            Settings.Add(selectedSeeds);
            selectedSeeds.SelectedValues.UnionWith(plantNameOverriden.Keys);

            selectedSeeds_dup = new MultiSelectSetting(
                "Duplicate Cards", typeof(PlantType))
            {
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = plantNameOverriden
            };

            Settings.Add(selectedSeeds_dup);
            selectedSeeds_dup.SelectedValues.UnionWith(plantNameOverriden.Keys);

            CustomCDSetting = new FloatSetting("Custom CD Multiplier", 0.01f, 100, customCD,2);
            Settings.Add(CustomCDSetting);

        }

        // Mod Logic

        private Dictionary<CardUI,float> originalCD = new Dictionary<CardUI,float>();
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull || InGameUI.Instance == null || InGameUI.Instance.Cards.Count == 0)
            {
                originalCD.Clear();
                return;
            }

            float customCD = CustomCDSetting.Value;

            foreach(CardUI card in InGameUI.Instance.Cards)
            {
                if (card == null) continue;

                // Save original cd
                if (!originalCD.ContainsKey(card))
                {
                    originalCD[card] = card.fullCD;

                }

                // If card is original
                if (!card.isExtra)
                {
                    if (!selectedSeeds.IsSelected((int)card.thePlantType)) continue;

                    if (!selectedSeeds.IsSelected((int)card.thePlantType) &&
                        originalCD.ContainsKey(card))
                    {
                        card.fullCD = originalCD[card];
                        originalCD.Remove(card);
                    }
                }

                // If card is duplicate
                if (card.isExtra)
                {
                    if (!selectedSeeds_dup.IsSelected((int)card.thePlantType)) continue;

                    if (!selectedSeeds_dup.IsSelected((int)card.thePlantType) &&
                        originalCD.ContainsKey(card))
                    {
                        card.fullCD = originalCD[card];
                        originalCD.Remove(card);
                    }
                }

                

                // Full CD if slider is at max
                if (customCD == 100)
                {
                    card.fullCD = 0;
                    continue;
                }

                if (card.fullCD != originalCD[card] / customCD)
                {
                    card.fullCD = originalCD[card] / customCD;
                }

            }

        }

        public override void OnDisable()
        {
            foreach(var card in originalCD)
            {
                card.Key.fullCD = card.Value;
            }

            originalCD.Clear();
        }


        [HarmonyPatch(typeof(CardUI))]
        public static class CardUIPatch
        {
            [HarmonyPatch(nameof(CardUI.CDUpdate))]
            [HarmonyPostfix]
            public static void CCDUpdatePostfix(CardUI __instance)
            {
                if (__instance.CD > __instance.fullCD)
                {
                    __instance.CD = __instance.fullCD;
                }
            }
        }

    }

}
