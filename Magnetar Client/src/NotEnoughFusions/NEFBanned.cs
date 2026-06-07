using Il2Cpp;
using System.Collections.Generic;

namespace Magnetar_Client.NEF.Data
{
    public static class NEFBanned
    {

        public static HashSet<int> InitBannedIDs = new HashSet<int>
        {
            233,247,250,
            246,257,258,259,260,261,262,263,264,265,266,267,268,350,351,352,353,354,
            355,356,357,358,359,360,361,362,363,364,365,366,367,368,219,220,221,230,231,232,-1,
            (int)PlantType.ZombieEndoFlame,(int)PlantType.UltimateRedLunar,
            (int)PlantType.TorchFireNut, (int)PlantType.HolographicPlant,
            (int)PlantType.HypnoCattailGirl_land,(int)PlantType.Ulti_cherryGatling  
            
        };

        public static HashSet<int> InitHiddenIDs = new HashSet<int>
        {
            (int)PlantType.SpreadFume,(int)PlantType.SpreadScaredyShroom,(int)PlantType.DiamondImitater,
            (int)PlantType.DiamondPotatoNut,(int)PlantType.LuckyBlover,(int)PlantType.XXSPot
        };

        
        public static void AddToBanList(int plantID)
        {
            NEFData.BannedPlants.Add(plantID);
        }

        public static void RemoveFromBanList(int plantID)
        {
            if (NEFData.BannedPlants.Contains(plantID))
                NEFData.BannedPlants.Remove(plantID);
        }

        public static void InitBan()
        {
            foreach (int id in InitBannedIDs)
            {
                AddToBanList(id);
            }
        }

        public static void AddToHiddenList(int plantID)
        {
            NEFData.SearchHiddenPlants.Add(plantID);
        }

        public static void RemoveFromHiddenList(int plantID)
        {
            if (NEFData.SearchHiddenPlants.Contains(plantID))
                NEFData.SearchHiddenPlants.Remove(plantID);
        }

        public static void InitHidden()
        {
            foreach (int id in InitHiddenIDs)
            {
                AddToBanList(id);
            }
        }

    }
}
