using static Magnetar_Client.Game.AppData;
using static MelonLoader.MelonLogger;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules
{
    public class SunHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Sun Hack";
        public override string Description { get; set; } = "Gives you unlimited sun.";
        public override string SearchHints { get; set; } = "unlimited sun infinite sun snu sum ifnite finite sun" +
            "ulimited sun limitless sun unrestricted sun endless sun sunmultiplier solarmultiplier Money Hack Money Cheat " +
            "sunboost sunbonus sunpowerup sonmultiplier sunmultiplyer sunmultipliar sunmultipier sunmultyplier sunmulltiplier" +
            " sunmultaplier lightmultiplier raymultiplier sunfactor sunamplifier sunintensifier sunenhancer solmultiplier sunx2";
        
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // The mod data

        public static SunHack instance;

        public BoolSetting UnlimitedSun;

        private int originalSunAmount = -853721;
        private IntSetting sunSetting;

        private BoolSetting preserveOriginalSetting;

        public BoolSetting SunMultipier;
        private FloatSetting sunMultiplierSetting;

        private static int _sunAmount = -947624;

        public SunHack()
        {
            instance = this;

            CreateCategory("Unlimited Sun");

            UnlimitedSun = new BoolSetting("Unlimited Sun", true);
            Settings.Add(UnlimitedSun);

            sunSetting = new IntSetting("Sun Amount", 0, 99999, 99999);
            Settings.Add(sunSetting);

            preserveOriginalSetting = new BoolSetting("Preserve Original", true);
            Settings.Add(preserveOriginalSetting);

            EndCategory();
            CreateCategory("Sun Multiplier");

            SunMultipier = new BoolSetting("Sun Multiplier", false);
            Settings.Add(SunMultipier);

            sunMultiplierSetting = new FloatSetting("Multiplier", -100, 100, 2);
            Settings.Add(sunMultiplierSetting);

            EndCategory();

        }

        // Mod Logic

        public override void OnDisable() 
        { 
            if (BoardInstanceIsNull) return;

            if (UnlimitedSun.Value)
            {
                if (originalSunAmount > 0 && preserveOriginalSetting.Value)
                {
                    board.theSun = originalSunAmount;
                    originalSunAmount = -853721;
                }
            }

            _sunAmount = -947624;

        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) {_sunAmount = -947624; return; }

            if (UnlimitedSun.Value)
            {
                if (originalSunAmount == -853721) originalSunAmount = board.theSun;
                board.theSun = sunSetting.Value;
            }
            
            else if (SunMultipier.Value)
            {
                int Sun = board.theSun;

                if (_sunAmount == -947624 || Sun == sunSetting.Value) _sunAmount = Sun;

                if (Sun != _sunAmount)
                {
                    if ((Sun - _sunAmount) > 0)
                        board.theSun += (int)((Sun - _sunAmount) * (sunMultiplierSetting.Value - 1));

                    _sunAmount = board.theSun;

                }
            }
        }

        
    }

    public class MoneyHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Money Hack";
        public override string Description { get; set; } = "Dave will become rich with this one.";
        public override string SearchHints { get; set; } = "unlimited money infinite money mny mney ifnite finite" +
            "ulimited limitless unrestricted endless moneymultiplier cashmultiplier moneyboost moneybonus moneyincrease" +
            " moneymultiplyer monymultiplier moenymultiplier moneymultipier moneymultaplier moneymulltiplier moneymultipyler" +
            " wealthmultiplier richesmultiplier coinmultiplier dollarmultiplier currencymultiplier moneyfactor moneyx2 cashboost";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // mod data

        public static MoneyHack instance;

        public BoolSetting UnlimitedMoney;
        private int originalMoneyAmount = -853721;
        private IntSetting moneySetting;

        private BoolSetting preserveOriginalSetting;

        public BoolSetting MoneyMultiplier;

        public FloatSetting moneyMultiplierSetting;
        private static int _moneyAmount = -947624;
        public override bool Active { get; set; } = false;

        public MoneyHack()
        {
            instance = this;

            CreateCategory("Unlimited Money");

            UnlimitedMoney = new BoolSetting("Unlimited Money",true);
            Settings.Add(UnlimitedMoney);

            moneySetting = new IntSetting("Money Amount", 0, 99999, 9999999);
            Settings.Add(moneySetting);

            preserveOriginalSetting = new BoolSetting("Preserve Original", true);
            Settings.Add(preserveOriginalSetting);

            EndCategory();
            CreateCategory("Money Multiplier");

            MoneyMultiplier = new BoolSetting("Money Multiplier", false);
            Settings.Add(MoneyMultiplier);

            moneyMultiplierSetting = new FloatSetting("Multiplier", -100, 100, 2);
            Settings.Add(moneyMultiplierSetting);

            EndCategory();
        }

        // Mod Logic
        public override void OnDisable()
        {
            if (BoardInstanceIsNull) return;

            if (UnlimitedMoney.Value)
            {
                if (originalMoneyAmount > 0 && preserveOriginalSetting.Value)
                {
                    board.theMoney = originalMoneyAmount;
                    originalMoneyAmount = -853721;
                }
            }

            _moneyAmount = -947624;

        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) { _moneyAmount = -947624; return; }

            if (UnlimitedMoney.Value)
            {
                if (originalMoneyAmount == -853721) originalMoneyAmount = board.theMoney;
                board.theMoney = moneySetting.Value;
            }

            else if (MoneyMultiplier.Value)
            {
                int Money = board.theMoney;

                if (_moneyAmount == -947624 || Money == moneySetting.Value) _moneyAmount = Money;

                if (Money != _moneyAmount)
                {
                    if ((Money - _moneyAmount) > 0)
                        board.theMoney += (int)((Money - _moneyAmount) * (moneyMultiplierSetting.Value - 1));

                    _moneyAmount = board.theMoney;

                }
            }
        }


    }
    
    public class PointsHack : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Points Hack";
        public override string Description { get; set; } = "Now you can purchase any modifier.";
        public override string SearchHints { get; set; } = "unlimited points infinite points pts currency gold ifnite finite" +
            "ulimited limitless unrestricted endless pointsmultiplier pointmultiplier ptsmultiplier scoremultiplier scoreboost" +
            " pointsboost pointmultiplyer pointsmultiplyer pointmultipier pointsmultipier pointsmultyplier pointmultyplier" +
            " scoreincrease pointsbonus pointbonus pointx2 ptsbonus pointsfactor scorefactor";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data

        public static PointsHack instance;

        public BoolSetting UnlimitedPoints;

        private float originalPointsAmount = -853721;
        private FloatSetting pointsSetting;
        private BoolSetting preserveOriginalSetting;

        public BoolSetting PointsMultiplier;

        private FloatSetting pointsMultiplierSetting;
        private static float _pointsAmount = -947624.35f;
        public override bool Active { get; set; } = false;

        public PointsHack()
        {
            instance = this;

            CreateCategory("Unlimited Points");

            UnlimitedPoints = new BoolSetting("Unlimited Points", true);
            Settings.Add(UnlimitedPoints);

            pointsSetting = new FloatSetting("Points Amount", 0, 9999999, 9999999);
            Settings.Add(pointsSetting);

            preserveOriginalSetting = new BoolSetting("Preserve Original", true);
            Settings.Add(preserveOriginalSetting);

            EndCategory();
            CreateCategory("Points Multiplier");

            PointsMultiplier = new BoolSetting("Points Multiplier", false);
            Settings.Add(PointsMultiplier);

            pointsMultiplierSetting = new FloatSetting("Multiplier", -100, 100, 2);
            Settings.Add(pointsMultiplierSetting);

            EndCategory();
        }

        // Mod Logic
        public override void OnDisable()
        {
            if (BoardInstanceIsNull) return;

            if (UnlimitedPoints.Value)
            {
                if (originalPointsAmount > 0 && preserveOriginalSetting.Value)
                {
                    board.thePoints = originalPointsAmount;
                    originalPointsAmount = -853721;
                }
            }

            _pointsAmount = -947624.35f;

        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) { _pointsAmount = -947624.35f; return; }

            if (UnlimitedPoints.Value)
            {
                if (originalPointsAmount == -853721) originalPointsAmount = board.theMoney;
                board.thePoints = pointsSetting.Value;
            }

            else if (PointsMultiplier.Value)
            {
                float Money = board.thePoints;

                if (_pointsAmount == -947624.35f || Money == pointsSetting.Value) _pointsAmount = Money;

                if (Money != _pointsAmount)
                {
                    if ((Money - _pointsAmount) > 0)
                        board.thePoints += (int)((Money - _pointsAmount) * (pointsMultiplierSetting.Value - 1));

                    _pointsAmount = board.thePoints;

                }
            }
        }


    }
    
}
