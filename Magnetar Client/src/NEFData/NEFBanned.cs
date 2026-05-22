using Il2Cpp;
using Magnetar_Client.Core;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;

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
        };



        public static void AddToList(int plantID)
        {
            NEFManager.BannedPlants.Add(plantID);
        }

        public static void RemoveFromList(int plantID)
        {
            if (NEFManager.BannedPlants.Contains(plantID))
                NEFManager.BannedPlants.Remove(plantID);
        }

        public static void InitBan()
        {
            foreach (int id in InitBannedIDs)
            {
                AddToList(id);
            }
        }

    }
}
