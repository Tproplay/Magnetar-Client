using HarmonyLib;
using System.Linq;
using System.Collections.Generic;
using Magnetar_Client.Utils;
using static Magnetar_Client.Game.AppData;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class CustomCDGlove : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Glove CD";
        public override string Description { get; set; } = "Modifies the Ingame Glove's Cooldown.";
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
            CreateCategory("General");

            CustomCDSetting = new FloatSetting("Custom Glove Cooldown", 0, 999, CustomCD);
            Settings.Add(CustomCDSetting);
            CustomCD = CustomCDSetting.Value;

            EndCategory();
            CreateCategory("Extra");

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginal);
            Settings.Add(preserveOriginalSetting);
            preserveOriginal = preserveOriginalSetting.Value;

            resetCDonEnableSetting = new BoolSetting("Reset CD on Enable", resetCDonEnable);
            Settings.Add(resetCDonEnableSetting);
            resetCDonEnable = resetCDonEnableSetting.Value;

            EndCategory();
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
        public override string Description { get; set; } = "Modifies the Ingame Hammer's Cooldown.";
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

            CreateCategory("General");

            CustomCDSetting = new FloatSetting("Custom Hammer Cooldown", 0, 999, CustomCD);
            Settings.Add(CustomCDSetting);
            CustomCD = CustomCDSetting.Value;

            EndCategory();
            CreateCategory("Extra");

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginal);
            Settings.Add(preserveOriginalSetting);
            preserveOriginal = preserveOriginalSetting.Value;

            resetCDonEnableSetting = new BoolSetting("Reset CD on Enable", resetCDonEnable);
            Settings.Add(resetCDonEnableSetting);
            resetCDonEnable = resetCDonEnableSetting.Value;

            EndCategory();

        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (Hammer.Instance == null)
            {
                originalCD = -1f;
                return;
            }

            // Save Original CD
            if (originalCD == -1f) originalCD = Hammer.Instance.fullCD;

            // BugFix: If the player has a higher CD than the one we set, we set it to our custom CD so
            // it doesn't take longer than intended to use the hammer again. This can happen if the player
            // has a CD increasing item and they enable this mod while the CD is still active.
            if (Hammer.Instance.CD > CustomCDSetting.Value)
            {
                Hammer.Instance.CD = CustomCDSetting.Value;
                Hammer.Instance.CDUpdate();
            }

            Hammer.Instance.fullCD = CustomCDSetting.Value;


        }

        public override void OnDisable()
        {

            if (Hammer.Instance == null) return;

            if (originalCD >= 0 && preserveOriginalSetting.Value)
            {
                Hammer.Instance.fullCD = originalCD;
            }
            originalCD = -1f;
        }

        public override void OnEnable()
        {
            if (Hammer.Instance == null) return;
            if (resetCDonEnableSetting.Value)
            {
                Hammer.Instance.CD = CustomCDSetting.Value;
            }
        }
    }

    /*
    public class CustomCDWheel : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Wheel Barrow CD";
        public override string Description { get; set; } = "Modifies the Ingame Wheel Barrow's Cooldown.";
        public override string SearchHints { get; set; } = "nohammercd hammercooldown customhammercd hammercd" +
            " zerohammercd nohammercoolndown hammercooldownreset fasthammer instanthammer hammerbuff hammercdmod " +
            "hammercdchanger hammercdremover hammercolldown hammercooldon hammercooldwn hamrcd hammercooldoun " +
            "hammercdtimer hammercdreducion hammercooldownreduction hammerfast hammerready hammerunlimited hammerinfinite " +
            "hammerinterval hammerperiod hammerspeed hammerspam hammerfrequency";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static CustomCDWheel instance;

        public float CustomCD = 0;
        public FloatSetting CustomCDSetting;

        private static float originalCD = -1f;

        public bool preserveOriginal = true;
        public BoolSetting preserveOriginalSetting;

        public bool resetCDonEnable = false;
        public BoolSetting resetCDonEnableSetting;

        public override bool Active { get; set; } = false;

        public CustomCDWheel()
        {
            instance = this;

            CreateCategory("General");

            CustomCDSetting = new FloatSetting("Custom Wheel Barrow Cooldown", 0, 999, CustomCD);
            Settings.Add(CustomCDSetting);
            CustomCD = CustomCDSetting.Value;

            EndCategory();
            CreateCategory("Extra");

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginal);
            Settings.Add(preserveOriginalSetting);
            preserveOriginal = preserveOriginalSetting.Value;

            resetCDonEnableSetting = new BoolSetting("Reset CD on Enable", resetCDonEnable);
            Settings.Add(resetCDonEnableSetting);
            resetCDonEnable = resetCDonEnableSetting.Value;

            EndCategory();

        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (wheel==null)
            {
                originalCD = -1f;
                return;
            }
            

            // Save Original CD
            if (originalCD == -1f) originalCD = wheel.fullCD;

            // BugFix: If the player has a higher CD than the one we set, we set it to our custom CD so
            // it doesn't take longer than intended to use the hammer again. This can happen if the player
            // has a CD increasing item and they enable this mod while the CD is still active.
            if (wheel.CD > CustomCDSetting.Value)
            {
                wheel.CD = CustomCDSetting.Value;
                wheel.CDUpdate();
            }

            wheel.fullCD = CustomCDSetting.Value;


        }

        public override void OnDisable()
        {

            if (wheel == null) return;

            if (originalCD >= 0 && preserveOriginalSetting.Value)
            {
                wheel.fullCD = originalCD;
            }
            originalCD = -1f;
        }

        public override void OnEnable()
        {
            if (wheel == null) return;
            if (resetCDonEnableSetting.Value)
            {
                wheel.CD = CustomCDSetting.Value;
            }
        }
    }
    */

    public class CustomCDCards : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Cards CD";
        public override string Description { get; set; } = "Modifies the Ingame SeedSlot Cards's Cooldown.";
        public override string SearchHints { get; set; } = "nocardscd cardscooldown customcardscd cardscd " +
            "zerocardscd nocardcoolndown cardcooldownreset fastcards instantcards cardbuff cardcdmod cardcdchanger " +
            "cardcdremover cardcolldown cardcooldon cardcooldwn cardcd cardcooldoun cardcdtimer cardcdreducion " +
            "cardcooldownreduction cardfast cardready cardunlimited cardinfinite cardinterval cardperiod cardspeed " +
            "cardspam cardfrequency";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static CustomCDCards instance;

        public FloatSetting CustomCDSetting;

        public MultiSelectSetting selectedSeeds;
        public MultiSelectSetting selectedSeeds_dup;

        public CustomCDCards()
        {
            instance = this;

            CreateCategory("General");

            selectedSeeds = new MultiSelectSetting(
                "Cards", typeof(PlantType))
            {
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = TranslatedNames(typeof(PlantType))
            };

            Settings.Add(selectedSeeds);
            selectedSeeds.Options.Keys.ToList().ForEach(selectedSeeds.Select);

            selectedSeeds_dup = new MultiSelectSetting(
                "Duplicate Cards", typeof(PlantType))
            {
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = TranslatedNames(typeof(PlantType))
            };

            Settings.Add(selectedSeeds_dup);
            selectedSeeds_dup.Options.Keys.ToList().ForEach(selectedSeeds_dup.Select);

            CustomCDSetting = new FloatSetting("Custom CD Multiplier", 0.01f, 10, 10,2);
            Settings.Add(CustomCDSetting);

            EndCategory();

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
