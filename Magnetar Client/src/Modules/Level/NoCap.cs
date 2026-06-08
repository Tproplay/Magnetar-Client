using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class NoCap : Module
    {
        // Mod Info
        public override string Name { get; set; } = "No Cap";
        public override string Description { get; set; } = "Bypasses the ingame sun/money cap";
        public override string SearchHints { get; set; } = "nocap suncap moneycap bypasscap capremove infinite " +
            "sun money unlimitedsun unlimitedmoney capbreak nosuncap nomoneycap caplimit moneyfix sunfix capoverride" +
            " unlimitedcurrency nosunlimit nomoneylimit capbypass maxsun maxmoney currencycap removecap moneycheat" +
            " suncheat nocapmod";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static NoCap instance;

        public IntSetting SunLimit;
        public IntSetting MoneyLimit;

        private static int originalSunLimit = -1;
        private static int originalMoneyLimit = -1;

        public NoCap() 
        { 
            instance = this;

            CreateCategory("General");

            SunLimit = new IntSetting("Sun Cap", 0, 2147483647, 2147483647);
            AddSettings(SunLimit);

            MoneyLimit = new IntSetting("Money Cap", 0, 2147483647, 2147483647);
            AddSettings(MoneyLimit);

            EndCategory();
        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) { originalMoneyLimit = -1;originalSunLimit = -1; return; }

            if (board.maxSun != SunLimit.Value)
                board.maxSun = SunLimit.Value;
            if (board.maxMoney != MoneyLimit.Value)
                board.maxMoney = MoneyLimit.Value;
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

    }
}
