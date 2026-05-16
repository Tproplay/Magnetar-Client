using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class UnlimitedSun : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Sun";
        public override string SearchHints { get; set; } = "unlimited sun infinite sun snu sum ifnite finite sun" +
            "ulimited sun limitless sun unrestricted sun endless sun";
        public override string Description { get; set; } = "Gives you unlimited sun.";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // The mod data

        public static UnlimitedSun instance;

        private readonly int sunAmount = 99999;
        private int originalSunAmount = -853721;
        private IntSetting sunSetting;

        private readonly bool preserveOriginl = true;
        private BoolSetting preserveOriginalSetting;


        public UnlimitedSun()
        {
            instance = this;

            sunSetting = new IntSetting("Sun Amount", 0, 99999, sunAmount);
            Settings.Add(sunSetting);

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginl);
            Settings.Add(preserveOriginalSetting);

        }

        // Mod Logic

        public override void OnDisable() 
        { 
            if (BoardInstanceIsNull) return;

            if (originalSunAmount >0 && instance.preserveOriginalSetting.Value)
            {
                board.theSun = originalSunAmount;
                originalSunAmount = -853721;
            }
        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;
            if (originalSunAmount == -853721) originalSunAmount = board.theSun;


            board.theSun = instance.sunSetting.Value;
        }

        
    }
    public class SunMultiplier : Module
    {
        
        // Mod Info
        public override string Name { get; set; } = "Sun Multiplier";
        public override string Description { get; set; } = "Multiplies the sun you get by a certain amount.";
        public override string SearchHints { get; set; } = "sunmultiplier solarmultiplier sunboostsunbonussunpowerupsonmultiplier" +
            " sunmultiplyer sunmultipliar sunmultipier sunmultyplier sunmulltiplier sunmultaplier lightmultiplier raymultiplier" +
            "sunfactor sunamplifier sunintensifier sunenhancer solmultiplier sunx2";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // The mod data

        private readonly float sunMultiplier = 2;
        private FloatSetting sunMultiplierSetting;

        public SunMultiplier()
        {
            sunMultiplierSetting = new FloatSetting("Multiplier", -100, 100, sunMultiplier);
            Settings.Add(sunMultiplierSetting);
        }

        // Mod Logic
        private static int _sunAmount = -947624;
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) { _sunAmount = -947624; return; }

            int Sun = board.theSun;

            if (_sunAmount== -947624) _sunAmount = Sun;
            
            if (Sun != _sunAmount)
            {
                if ((Sun - _sunAmount)>0)
                    board.theSun += (int)((Sun - _sunAmount)*(sunMultiplierSetting.Value - 1));

                _sunAmount = board.theSun;
            }

        }

        public override void OnDisable()
        {
            _sunAmount = -947624;
        }

        


    }

    public class UnlimitedMoney : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Money";
        public override string Description { get; set; } = "Gives you unlimited money.";
        public override string SearchHints { get; set; } = "unlimited money infinite money mny mney ifnite finite" +
            "ulimited limitless unrestricted endless";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // mod data

        public static UnlimitedMoney instance;

        private readonly int moneyAmount = 9999999;
        private int originalMoneyAmount = -853721;
        private IntSetting moneySetting;

        private readonly bool preserveOriginl = true;
        private BoolSetting preserveOriginalSetting;


        public override bool Active { get; set; } = false;

        public UnlimitedMoney()
        {
            instance = this;
            moneySetting = new IntSetting("Money Amount", 0, 99999, moneyAmount);
            Settings.Add(moneySetting);

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginl);
            Settings.Add(preserveOriginalSetting);
        }

        // Mod Logic
        public override void OnDisable()
        {
            if (BoardInstanceIsNull || !instance.preserveOriginalSetting.Value) return;

            board.theMoney = originalMoneyAmount;
            originalMoneyAmount = -853721;
        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;
            if (originalMoneyAmount == -853721) originalMoneyAmount = board.theMoney;

            board.theMoney = instance.moneySetting.Value;
        }


    }
    public class MoneyMultiplier : Module
    {

        // Mod Info
        public override string Name { get; set; } = "Money Multiplier";
        public override string Description { get; set; } = "Multiplies the money you get by a certain amount.";
        public override string SearchHints { get; set; } = "moneymultiplier cashmultiplier moneyboost moneybonus " +
            "moneyincrease moneymultiplyer monymultiplier moenymultiplier moneymultipier moneymultaplier moneymulltiplier " +
            "moneymultipyler wealthmultiplier richesmultiplier coinmultiplier dollarmultiplier currencymultiplier " +
            "moneyfactor moneyx2 cashboost";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data

        private readonly float moneyMultiplier = 2;
        private FloatSetting moneyMultiplierSetting;
        public override bool Active { get; set; } = false;


        public MoneyMultiplier()
        {
            moneyMultiplierSetting = new FloatSetting("Multiplier", -100, 100, moneyMultiplier);
            Settings.Add(moneyMultiplierSetting);
        }

        // Mod Logic

        private static int _moneyAmount = -947624;
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) { _moneyAmount = -947624; return; }

            int Money = board.theMoney;

            if (_moneyAmount == -947624) _moneyAmount = Money;

            if (Money != _moneyAmount)
            {
                if ((Money - _moneyAmount) > 0)
                    board.theMoney += (int)((Money - _moneyAmount) * (moneyMultiplierSetting.Value -1));

                _moneyAmount = board.theMoney;
            }

        }

        public override void OnDisable()
        {
            _moneyAmount = -947624;
        }
    }

    public class UnlimitedPoints : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Points";
        public override string Description { get; set; } = "Gives you unlimited points.";
        public override string SearchHints { get; set; } = "unlimited points infinite points pts currency gold ifnite finite" +
            "ulimited limitless unrestricted endless";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data

        private readonly float pointsAmount = 9999999;
        private float originalPointsAmount = -853721;
        private FloatSetting pointsSetting;

        private readonly bool preserveOriginl = true;
        private BoolSetting preserveOriginalSetting;

        public override bool Active { get; set; } = false;

        public UnlimitedPoints()
        {
            pointsSetting = new FloatSetting("Points Amount", 0, 9999999, pointsAmount);
            Settings.Add(pointsSetting);

            preserveOriginalSetting = new BoolSetting("Preserve Original", preserveOriginl);
            Settings.Add(preserveOriginalSetting);
        }

        // Mod Logic
        public override void OnDisable()
        {
            if (BoardInstanceIsNull || !preserveOriginalSetting.Value) return;

            board.thePoints = originalPointsAmount;
            originalPointsAmount = -853721;
        }

        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) return;
            if (originalPointsAmount == -853721) originalPointsAmount = board.thePoints;

            board.thePoints = pointsSetting.Value;
        }


    }
    public class PointsMultiplier : Module
    {

        // Mod Info
        public override string Name { get; set; } = "Points Multiplier";
        public override string Description { get; set; } = "Multiplies the points you get by a certain amount.";
        public override string SearchHints { get; set; } = "pointsmultiplier pointmultiplier ptsmultiplier " +
            "scoremultiplier scoreboost pointsboost pointmultiplyer pointsmultiplyer pointmultipier pointsmultipier" +
            "pointsmultyplier pointmultyplier scoreincrease pointsbonus pointbonus pointx2 ptsbonus pointsfactor scorefactor";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod data

        private readonly float pointsMultiplier = 2;
        private FloatSetting pointsMultiplierSetting;

        public PointsMultiplier()
        {
            pointsMultiplierSetting = new FloatSetting("Multiplier", -100, 100, pointsMultiplier);
            Settings.Add(pointsMultiplierSetting);
        }

        // Mod Logic

        private static float _pointsAmount = -947624.35f;
        public override void OnUpdateActive()
        {
            if (BoardInstanceIsNull) { _pointsAmount = -947624.35f; return; }

            float Points = board.thePoints;

            if (_pointsAmount == -947624.35f) _pointsAmount = Points;

            if (Points != _pointsAmount)
            {
                if ((Points - _pointsAmount) > 0)
                    board.thePoints += (Points - _pointsAmount) * (pointsMultiplierSetting.Value - 1);

                _pointsAmount = board.thePoints;
            }

        }

        public override void OnDisable()
        {
            _pointsAmount = -947624.35f;
        }
    }
}
