using HarmonyLib;

using Magnetar_Client.UI.Setting;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
#elif BEPINEX || RELEASE_BEPINEX
using GameLevel.RogueShooting;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Modules;

public class CurrencyHack : Module
{
    // Mod Info

    public override string Name { get; set; } = "Currency Hack";
    public override string Description { get; set; } = "Allows you to set a custom amount of currency.";
    public override string SearchHints { get; set; } = "currencyhack moneyhack coinchanger sunhack currencyeditor coinmodifier" +
        " moneyeditor currencyconfig goldhack cashmod coinsmanager sunmodifier currencyamount walleteditor fundsmodifier " +
        "customcurrency wealthhack currencycheat coincheat suneditor economyeditor moneymanager";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

    // Mod Data

    public static CurrencyHack instance;

    public IntSetting theGodsCoins = new("The Gods Coins", 0, 99999, 0) 
    { 
        IsDisabled = true, 
        InstantUpdate = true,
        OnValueChanged = (val) =>
        {
            GameAPP.config.shootingData.godCoins = val;
        }
    };
    public IntSetting treasureMoney = new("Harvest Mode Money", 0, 999999, 0)
    {
        IsDisabled = true,
        InstantUpdate = true,
        OnValueChanged = (val) =>
        {
            TreasureData.treasureMoney = val;
        }
    };

    public CurrencyHack()
    {
        instance = this;

        CreateCategory("General");
        AddSettings(theGodsCoins, treasureMoney);
        EndCategory();
    }

    // Mod Logic

    public override void OnEnable()
    {
        theGodsCoins.IsDisabled = false;
        treasureMoney.IsDisabled = false;
    }

    public override void OnDisable()
    {
        theGodsCoins.IsDisabled = true;
        treasureMoney.IsDisabled = true;
    }

    public override void OnSettingUpdate()
    {
        theGodsCoins.SetDirect(GameAPP.config.shootingData.godCoins);
        treasureMoney.SetDirect(TreasureData.treasureMoney);
    }
}