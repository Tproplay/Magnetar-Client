using HarmonyLib;
using Il2Cpp;
using Il2CppRhythmGame;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magnetar_Client.Modules
{
    public class BetterOsu : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Better Osu";
        public override string Description { get; set; } = "Makes Explode-O-su Better.\nIncreases Stats of Bullets as the current combo Increases.";
        public override string SearchHints { get; set; } = "betterosu osuplus osupro osuextended improvedosu bestosu " +
            "osufix osutweak osulite osupremuim betterosuo betterossu betterosuu bedterosu betertosu bestterosu superosu " +
            "eliteosu osuremastered perfectosu osunext osucustom osuoptimized osurefined osuadvanced osutools osugameplay " +
            "osufaster osusmooth";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;

        // Mod data

        public static BetterOsu instance;
        
        public static int currentCombo = 0;

        private bool HelperPet = false;
        public BoolSetting HelperPetSetting;

        private int MultiplyBullets = 200;
        public IntSetting BulletsDamageIncreaseSetting;

        private bool RandomBullet = false;
        public BoolSetting RandomBulletSetting;

        public List<int> preselected = Enum.GetValues(typeof(BulletType)).Cast<int>().ToList();
        public MultiSelectSetting selectBulletsSetting;


        public BetterOsu()
        {
            instance = this;

            BulletsDamageIncreaseSetting = new IntSetting("Increase Damage", 10, 1000,MultiplyBullets);
            Settings.Add( BulletsDamageIncreaseSetting );

            HelperPetSetting = new BoolSetting("Spawn Ice Queen Pet", HelperPet);
            Settings.Add( HelperPetSetting );

            RandomBulletSetting = new BoolSetting("Random Bullets", RandomBullet)
            {

            };
            Settings.Add( RandomBulletSetting );

            selectBulletsSetting = new MultiSelectSetting("Allowed bullets", typeof(BulletType));
            Settings.Add( selectBulletsSetting );
            selectBulletsSetting.SelectedValues.UnionWith( preselected );
        }

        static float DamageBuff = 1;
        static int SpawnedPets = 0;

        // Mod Logic
        public override void OnUpdateActive()
        {
            if (RhythmGameManager.Instance == null || Board.Instance == null)
            {
                originalDamage.Clear();
                currentCombo = 0;
                SpawnedPets = 0;
                return;
            }

            //MiniPet

            if (HelperPetSetting.Value)
            {
                if (SpawnedPets <= 0)
                {
                    Vector2 centerWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Camera.main.nearClipPlane));
                    MiniPet pet = MiniPet.SetPet(Board.Instance, centerWorldPos, PetType.PetSnowBoss);
                    SpawnedPets++;
                }
            }

            if (!HelperPetSetting.Value)
            {
                if (SpawnedPets > 0)
                {
                    MiniPet pet = GameObject.FindObjectOfType<MiniPet>();
                    UnityEngine.Object.Destroy(pet);
                    pet.gameObject.SetActive(false);
                    SpawnedPets--;
                }
            }

            // Bullet Damage
            currentCombo = RhythmGameManager.Instance.comboManager.currentCombo;
            DamageBuff = (float)currentCombo / (float)instance.BulletsDamageIncreaseSetting.Value;


        }

        public override void OnDisable()
        {
            currentCombo = 0;
            originalDamage.Clear();
        }

        static Dictionary<Bullet, int> originalDamage = new Dictionary<Bullet, int>();

        [HarmonyPatch(typeof(Il2Cpp.Bullet))]
        public static class BulletPatch
        {

            [HarmonyPatch(nameof(Bullet.Update))]
            [HarmonyPrefix]
            public static void UpdatePatch(Bullet __instance)
            {
                if (instance == null || Board.Instance == null || __instance == null) return;
                if (!instance.Active || !Board.Instance.boardTag.rhythmGame) return;

                if (instance.BulletsDamageIncreaseSetting.Value <= currentCombo)
                {

                    if (!originalDamage.ContainsKey(__instance))
                    {
                        originalDamage.Add(__instance, __instance.Damage);
                    }

                    if (DamageBuff > 1 && __instance.Damage != (int)(originalDamage[__instance] * DamageBuff))
                    {
                        __instance.Damage = (int)(originalDamage[__instance] * DamageBuff);
                    }
                }
            }

            [HarmonyPatch(nameof(Bullet.Die))]
            [HarmonyPostfix]
            public static void DiePatch(Bullet __instance)
            {
                if (originalDamage.ContainsKey(__instance))
                {
                    originalDamage.Remove(__instance);
                }
            }

        }

        [HarmonyPatch(typeof(CreateBullet))]
        public static class CreateBulletPatch
        {

            [HarmonyPatch(nameof(CreateBullet.SetBullet))]
            [HarmonyPrefix]
            public static void SetBulletPrefix(ref BulletType theBulletType, ref bool fromEnermy)
            {
                if (instance == null || Board.Instance == null) return;
                if (!instance.Active || !Board.Instance.boardTag.rhythmGame) return;
                if (!instance.RandomBulletSetting.Value || fromEnermy) return;
                if (theBulletType != BulletType.Bullet_superCherry) return;

                BulletType newType = theBulletType;

                if (instance.selectBulletsSetting.SelectedValues.Count == 1)
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.First();

                if (instance.selectBulletsSetting.SelectedValues.Count > 1)
                {
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.ElementAt(
                        UnityEngine.Random.RandomRangeInt(0, instance.selectBulletsSetting.SelectedValues.Count));
                }
                MelonLogger.Msg((int)newType);
                if (instance.selectBulletsSetting.SelectedValues.Contains((int)newType))
                    theBulletType = newType;
            }
        }

        [HarmonyPatch(typeof(Board),nameof(Board.Awake))]
        public static void BoardAwakePatch()
        {
            originalDamage.Clear();
            SpawnedPets = 0;
            currentCombo = 0;
        }

    }
}
