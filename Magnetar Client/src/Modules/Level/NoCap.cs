using static Magnetar_Client.Game.AppData;
using HarmonyLib;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class NoCap : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Cap";
        public override string Description { get; set; } = "Bypasses the ingame sun/money cap.";
        public override string SearchHints { get; set; } = "nocap suncap moneycap bypasscap capremove infinite " +
            "sun money unlimitedsun unlimitedmoney capbreak nosuncap nomoneycap caplimit moneyfix sunfix capoverride" +
            " unlimitedcurrency nosunlimit nomoneylimit capbypass maxsun maxmoney currencycap removecap moneycheat" +
            " suncheat nocapmod";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static NoCap instance;

        public IntSetting SunLimit;
        public IntSetting MoneyLimit;

        public int originalSunLimit = -1;
        public int originalMoneyLimit = -1;

        public NoCap() 
        { 
            instance = this;

            CreateCategory("General");

            SunLimit = new IntSetting("Sun Cap", 0, 1_000_000, int.MaxValue)
            {
                OnValueChanged = SetSunLimit
            };
            AddSettings(SunLimit);

            MoneyLimit = new IntSetting("Money Cap", 0, 1_000_000, int.MaxValue)
            {
                OnValueChanged = SetMoneyLimit
            };
            AddSettings(MoneyLimit);

            EndCategory();
        }

        // Mod Logic

        public override void OnEnable()
        {
            SetSunLimit(SunLimit.Value);
            SetMoneyLimit(MoneyLimit.Value);
        }

        public override void OnDisable()
        {
            if (BoardInstanceIsNull) { originalMoneyLimit = -1; originalSunLimit = -1; return; }

            if (originalSunLimit != -1)
            {
                board.maxSun = originalSunLimit;
                originalSunLimit = -1;
            }
            if (originalMoneyLimit != -1)
            {
                board.maxMoney = originalMoneyLimit;
                originalMoneyLimit = -1;
            }

        }

        void SetSunLimit(int value)
        {
            if (BoardInstanceIsNull) return;
            board.maxSun = value;
        }

        void SetMoneyLimit(int value)
        {
            if (BoardInstanceIsNull) return;
            board.maxMoney = value;
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Start))]
            [HarmonyPostfix]
            public static void StartPostfix(Board __instance)
            {
                if (instance == null) return;
                instance.originalSunLimit = __instance.maxSun;
                instance.originalMoneyLimit = __instance.maxMoney;

                if (instance.Active) instance.OnEnable();
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            public static void DiePostfix()
            {
                if (instance == null) return;

                instance.originalSunLimit = -1;
                instance.originalMoneyLimit = -1;
            }
        }

    }
}
