using System.Collections.Generic;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.NEF.Data;

public static class NEFBanned
{

    static HashSet<int> InitBannedIDs = new()
    {
        // Not a Plant
        234,247,
        246,257,258,259,260,
        219,220,221,230,231,232,-1,

        // EnumValueAsmResolver_002EDotNet_002ESerialized_002ESerializedConstant
        261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,
        
        // TD Plants
        350,351,352,353,354,355,356,357,358,359,360,361,362,363,364,365,366,367,368,
        
        // No Recipies
        (int)PlantType.HolographicPlant,

        // For future version
        (int)PlantType.Ulti_cherryGatling,

        // Duplicate
        (int)PlantType.UltimateRedLunar,
        (int)PlantType.MagicSnowPea2,
        (int)PlantType.HypnoCattailGirl_land,
        (int)PlantType.TorchFireNut,

        // Zombies
        (int)PlantType.Legion, (int)PlantType.SnowBird,
        (int)PlantType.ZombieEndoFlame, 

    };

    static HashSet<int> InitHiddenIDs = new()
    {
        (int)PlantType.SpreadFume,(int)PlantType.SpreadScaredyShroom,(int)PlantType.DiamondImitater,
        (int)PlantType.DiamondPotatoNut,(int)PlantType.LuckyBlover,(int)PlantType.XXSPot
    };

    #region Ban
    public static void AddToBanList(int plantID)
    {
        NEFData.BannedPlants.Add(plantID);
    }
    public static void AddToBanList(PlantType plantType) => AddToBanList((int)plantType);

    public static void RemoveFromBanList(int plantID)
    {
       NEFData.BannedPlants.Remove(plantID);
    }
    public static void RemoveFromBanList(PlantType plantType) => RemoveFromBanList((int)plantType);

    static void InitBan()
    {
        foreach (int id in InitBannedIDs)
        {
            AddToBanList(id);
        }
    }
    #endregion

    #region Hidden
    public static void AddToHiddenList(int plantID)
    {
        NEFData.SearchHiddenPlants.Add(plantID);
    }
    public static void AddToHiddenList(PlantType plantType) => AddToHiddenList((int)plantType);

    public static void RemoveFromHiddenList(int plantID)
    {
        NEFData.SearchHiddenPlants.Remove(plantID);
    }
    public static void RemoveFromHiddenList(PlantType plantType) => RemoveFromHiddenList((int)plantType);

    static void InitHidden()
    {
        foreach (int id in InitHiddenIDs)
        {
            AddToBanList(id);
        }
    }
    #endregion

    internal static void Init()
    {
        InitBan();
        InitHidden();
    }

}
