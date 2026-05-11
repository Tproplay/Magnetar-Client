using Il2Cpp;
using HarmonyLib;


namespace Magnetar_Client.Modules
{
    public class UnlimitedSun : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Sun";
        public override string SearchHints { get; set; } = "unlimited sun infinite sun snu sum ifnite finite sun" +
            "ulimited sun limitless sun unrestricted sun endless sun";
        public override string Description { get; set; } = "Gives you unlimited sun.";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // The mod data

        private int sunAmount = 99999;
        private int originalSunAmount = -853721;
        private IntSetting sunSetting;

        private bool preserveOriginl = true;
        private BoolSetting preserveOriginalSetting;


        public override bool Active { get; set; } = false;

        public UnlimitedSun()
        {
            var setting = new IntSetting("Sun Amount", 0, 99999, sunAmount);
            Settings.Add(setting);
            sunSetting = setting;

            var preserveSetting = new BoolSetting("Preserve Original", preserveOriginl);
            Settings.Add(preserveSetting);
            preserveOriginalSetting = preserveSetting;
        }

        // Mod Logic
        public override void OnEnable() 
        {
            if (Board.Instance == null) return;
            originalSunAmount = Board.Instance.theSun;
        }
        public override void OnDisable() 
        { 
            if (Board.Instance == null) return;

            if (preserveOriginalSetting.Value)
            {
                Board.Instance.theSun = originalSunAmount;
                originalSunAmount = -853721;
            }
        }

        public override void OnUpdateActive()
        {
            if (Board.Instance == null) return;
            if (originalSunAmount == -853721) originalSunAmount = Board.Instance.theSun;


            sunAmount = sunSetting.Value;
            Board.Instance.theSun = sunAmount;
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
        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // The mod data
        public static SunMultiplier Instance;

        private float sunMultiplier = 2;
        private FloatSetting sunMultiplierSetting;
        public override bool Active { get; set; } = false;

        
        public SunMultiplier()
        {
            Instance = this;
            var setting = new FloatSetting("Multiplier", -100, 100, sunMultiplier);
            Settings.Add(setting);
            sunMultiplierSetting = setting;
        }

        // Mod Logic

        [HarmonyPatch(typeof(Board), nameof(Board.GetSun))]
        [HarmonyPrefix]
        public static bool Prefix(ref float count)
        {
            if (Instance == null || !Instance.Active) return true;
            count = count*Instance.sunMultiplierSetting.Value;
            return true;
        }
    }

    public class UnlimitedMoney : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Money";
        public override string Description { get; set; } = "Gives you unlimited money.";
        public override string SearchHints { get; set; } = "unlimited money infinite money mny mney ifnite finite" +
            "ulimited limitless unrestricted endless";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // mod data

        private int moneyAmount = 9999999;
        private int originalMoneyAmount = -853721;
        private IntSetting moneySetting;

        private bool preserveOriginl = true;
        private BoolSetting preserveOriginalSetting;


        public override bool Active { get; set; } = false;

        public UnlimitedMoney()
        {
            var setting = new IntSetting("Money Amount", 0, 99999, moneyAmount);
            Settings.Add(setting);
            moneySetting = setting;
            var preserveSetting = new BoolSetting("Preserve Original", preserveOriginl);
            Settings.Add(preserveSetting);
            preserveOriginalSetting = preserveSetting;
        }

        // Mod Logic
        public override void OnEnable()
        {
            if (Board.Instance == null) return;
            originalMoneyAmount = Board.Instance.theMoney;
        }
        public override void OnDisable()
        {
            if (Board.Instance == null) return;

            if (preserveOriginalSetting.Value)
            {
                Board.Instance.theMoney = originalMoneyAmount;
                originalMoneyAmount = -853721;
            }
        }

        public override void OnUpdateActive()
        {
            if (Board.Instance == null) return;
            if (originalMoneyAmount == -853721) originalMoneyAmount = Board.Instance.theMoney;

            moneyAmount = moneySetting.Value;
            Board.Instance.theMoney = moneyAmount;
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
        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // Mod data
        public static MoneyMultiplier Instance;
        private float moneyMultiplier = 2;
        private FloatSetting moneyMultiplierSetting;
        public override bool Active { get; set; } = false;


        public MoneyMultiplier()
        {
            Instance = this; // Set the instance to this so we can access it in the Harmony patch
            var setting = new FloatSetting("Multiplier", -100, 100, moneyMultiplier);
            Settings.Add(setting);
            moneyMultiplierSetting = setting;
        }

        // Mod Logic

        [HarmonyPatch(typeof(Board), nameof(Board.GetMoney))]
        [HarmonyPrefix]
        public static bool Prefix(ref float count)
        {
            if (Instance == null || !Instance.Active) return true;
            count = count * Instance.moneyMultiplierSetting.Value;
            return true;
            
        }
    }

    public class UnlimitedPoints : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Points";
        public override string Description { get; set; } = "Gives you unlimited points.";
        public override string SearchHints { get; set; } = "unlimited points infinite points pts currency gold ifnite finite" +
            "ulimited limitless unrestricted endless";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // Mod data

        private float pointsAmount = 9999999;
        private float originalPointsAmount = -853721;
        private FloatSetting pointsSetting;

        private bool preserveOriginl = true;
        private BoolSetting preserveOriginalSetting;

        public override bool Active { get; set; } = false;

        public UnlimitedPoints()
        {
            var setting = new FloatSetting("Points Amount", 0, 9999999, pointsAmount);
            Settings.Add(setting);
            pointsSetting = setting;
            
            var preserveSetting = new BoolSetting("Preserve Original", preserveOriginl);
            Settings.Add(preserveSetting);
            preserveOriginalSetting = preserveSetting;
        }

        // Mod Logic
        public override void OnEnable()
        {
            if (Board.Instance == null) return;
            originalPointsAmount = Board.Instance.thePoints;
        }
        public override void OnDisable()
        {
            if (Board.Instance == null) return;

            if (preserveOriginalSetting.Value)
            {
                Board.Instance.thePoints = originalPointsAmount;
                originalPointsAmount = -853721;
            }
        }

        public override void OnUpdateActive()
        {
            if (Board.Instance == null) return;
            if (originalPointsAmount == -853721) originalPointsAmount = Board.Instance.thePoints;

            pointsAmount = pointsSetting.Value;
            Board.Instance.thePoints = pointsAmount;
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

        public override ModuleCategory Category { get; set; } = ModuleCategory.Board;

        // Mod data
        public static PointsMultiplier Instance;
        private float pointsMultiplier = 2;
        private FloatSetting pointsMultiplierSetting;
        public override bool Active { get; set; } = false;


        public PointsMultiplier()
        {
            Instance = this; // Set the instance to this so we can access it in the Harmony patch
            var setting = new FloatSetting("Multiplier", -100, 100, pointsMultiplier);
            Settings.Add(setting);
            pointsMultiplierSetting = setting;
        }

        // Mod Logic

        [HarmonyPatch(typeof(Board), nameof(Board.GetPoint))]
        [HarmonyPrefix]
        public static bool Prefix(ref float count)
        {
            if (Instance == null || !Instance.Active) return true;
            count = count * Instance.pointsMultiplierSetting.Value;
            return true;

        }
    }
}
