
using HarmonyLib;
using Magnetar_Client.UI.Setting;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

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

        GameData.Actions.OnBoardInstantiated += (board) =>
        {
            originalSunLimit = board.maxSun;
            originalMoneyLimit = board.maxMoney;
            if (Active) OnEnable();
        };

        GameData.Actions.OnBoardDestroyed += () =>
        {
            originalSunLimit = -1;
            originalMoneyLimit = -1;
        };

        GameData.Actions.OnBoardDied += () =>
        {
            originalSunLimit = -1;
            originalMoneyLimit = -1;
        };
    }

    // Mod Logic

    public override void OnEnable()
    {
        SetSunLimit(SunLimit.Value);
        SetMoneyLimit(MoneyLimit.Value);
    }

    public override void OnDisable()
    {
        if (GameData.BoardInstanceIsNull) { originalMoneyLimit = -1; originalSunLimit = -1; return; }

        if (originalSunLimit != -1)
        {
            GameData.BoardInstance.maxSun = originalSunLimit;
            originalSunLimit = -1;
        }
        if (originalMoneyLimit != -1)
        {
            GameData.BoardInstance.maxMoney = originalMoneyLimit;
            originalMoneyLimit = -1;
        }

    }

    void SetSunLimit(int value)
    {
        if (GameData.BoardInstanceIsNull) return;
        GameData.BoardInstance.maxSun = value;
    }

    void SetMoneyLimit(int value)
    {
        if (GameData.BoardInstanceIsNull) return;
        GameData.BoardInstance.maxMoney = value;
    }



}
