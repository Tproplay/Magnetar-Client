using HarmonyLib;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using MelonLoader;
using System.Collections.Generic;



#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class UnlimitedLawnMowers : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Unlimited Lawn Mowers";
        public override string Description { get; set; } = "Also provides cover against flying ships somehow";
        public override string SearchHints { get; set; } = "unlimitedlawnmowers lawnmoverhack lawnmoverunlimited" +
            " infinitelawnmowers multimower mowercheat mowerdefense flyingmower mowerprotection shipdefense unlimitedmovers " +
            "lawnmovermod mowerfix antimower mowerunlimited flyingdefense lawnmovers unlimitedmover covermovers " +
            "mowerbufflawnmoverunlimited infinite-mowers lawnmover-godmode mower-spam " +
            "endless-mowers lawn-cleaner-unlimited ship-protection mower-respawn instant-mover lawn-saver all-lane-defense " +
            "mower-cheat-code unlimited-lawn-protection ship-shield mower-unlimited-use";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        public static UnlimitedLawnMowers instance;


        public UnlimitedLawnMowers()
        {
            instance = this;
        }

        // Mod Logic

        public override void OnEnable()
        {
            if (BoardInstanceIsNull || CreateMower.Instance == null || board == null || board.mowerArray == null)
                return;
            bool[] rowHasMower = new bool[board.rowNum];

            for (int i = 0; i < board.mowerArray.Count; i++)
            {
                Mower m = board.mowerArray[i];
                if (m != null && m.theMowerRow >= 0 && m.theMowerRow < board.rowNum)
                {
                    rowHasMower[m.theMowerRow] = true;
                }
            }
            for (int i = 0; i < board.rowNum; i++)
            {
                if (!rowHasMower[i])
                {
                    CreateMower.Instance.SetMowerOnRoad(board.roadType[i], i);
                    for (int j = 0; j < board.mowerArray.Count; j++)
                    {
                        Mower newMower = board.mowerArray[j];
                        if (newMower != null && newMower.theMowerRow == i && !newMower.started)
                        {
                            newMower.transform.position = new UnityEngine.Vector3(
                                -6f,
                                newMower.transform.position.y,
                                newMower.transform.position.z
                            );
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardMowerCentralPatch
        {
            private static HashSet<int> processedMowerIDs = new HashSet<int>();

            [HarmonyPatch(nameof(Board.Update))]
            [HarmonyPostfix] 
            public static void BoardUpdatePostfix(Board __instance)
            {
                if (instance == null || !instance.Active || __instance == null || __instance.mowerArray == null)
                    return;

                for (int i = 0; i < __instance.mowerArray.Count; i++)
                {
                    Mower mower = __instance.mowerArray[i];
                    if (mower == null) continue;
                    if (mower.started)
                    {
                        int mowerId = mower.GetInstanceID();

                        if (!processedMowerIDs.Contains(mowerId))
                        {
                            processedMowerIDs.Add(mowerId);

                            int row = mower.theMowerRow;
                            CreateMower.Instance.SetMowerOnRoad(__instance.roadType[row], row);

                            Mower replacementMower = __instance.mowerArray.ToArray()
                                .Where(m => m != null && m.theMowerRow == row && !m.started)
                                .FirstOrDefault();

                            if (replacementMower != null)
                            {
                                replacementMower.transform.position = new Vector3(
                                    -6f,
                                    replacementMower.transform.position.y,
                                    replacementMower.transform.position.z
                                );
                                replacementMower.started = false;
                            }
                        }
                    }
                }
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPrefix]
            public static void DiePrefix()
            {
                processedMowerIDs.Clear();
            }
        }


    }
}
