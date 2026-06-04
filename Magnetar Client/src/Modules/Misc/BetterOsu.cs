using HarmonyLib;
using Il2Cpp;
using Il2CppRhythmGame;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Magnetar_Logger;

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
        
        public int currentCombo = 0;
        public BoolSetting HelperPetSetting;
        public MultiSelectSetting PetTypeSetting;

        public IntSetting BulletsDamageIncreaseSetting;

        private bool RandomBullet = false;
        public BoolSetting RandomBulletSetting;

        public MultiSelectSetting selectBulletsSetting;

#if DEBUG
        BoolSetting DebugMode;
#endif


        public BetterOsu()
        {
            instance = this;

            BulletsDamageIncreaseSetting = new IntSetting("Increase Damage", 1, 500,50);
            
            HelperPetSetting = new BoolSetting("Spawn Helper Pet", false);

            var namesPets = Translator.TranslateEnum(typeof(PetType));

            foreach (var name in namesPets)
            {
                namesPets[name.Key] = $"{name.Value} ({name.Key})";
            }

            PetTypeSetting = new MultiSelectSetting("Pet Type", typeof(PetType))
            {
                MaxSelection = 1,
                CustomNames = namesPets
            };
            PetTypeSetting.Select((int)PetType.PetSnowBoss);

            RandomBulletSetting = new BoolSetting("Random Bullets", RandomBullet);

            var bulletNames = Translator.TranslateEnum(typeof(BulletType));

            foreach (var name in bulletNames)
            {
                bulletNames[name.Key] = $"{bulletNames[name.Key]} ({name.Key})";
            }

            selectBulletsSetting = new MultiSelectSetting("Allowed bullets", typeof(BulletType))
            {
                CustomNames = bulletNames
            };
            selectBulletsSetting.SelectedValues.UnionWith(bulletNames.Keys);

#if DEBUG
            DebugMode = new BoolSetting("DebugMode", false);
#endif

            Settings.Add(BulletsDamageIncreaseSetting);
            Settings.Add(HelperPetSetting);
            Settings.Add(PetTypeSetting);
            Settings.Add(RandomBulletSetting);
            Settings.Add( selectBulletsSetting );
#if DEBUG
            Settings.Add(DebugMode);
#endif

        }

        public override void OnLanguageChanged()
        {
            var namesPets = Translator.TranslateEnum(typeof(PetType));

            foreach (var name in namesPets)
            {
                namesPets[name.Key] = $"{name.Value} ({name.Key})";
            }

            PetTypeSetting.CustomNames = namesPets;

            var bulletNames = Translator.TranslateEnum(typeof(BulletType));

            foreach (var name in bulletNames)
            {
                bulletNames[name.Key] = $"{bulletNames[name.Key]} ({name.Key})";
            }

            selectBulletsSetting.CustomNames = bulletNames;
        }

        float DamageBuff = 1;
        public int SpawnedPets = 0;

        // Mod Logic

        public void ResetData()
        {
            originalDamage.Clear();
            currentCombo = 0;
            SpawnedPets = 0;
        }

        public override void OnUpdateActive()
        {
            RhythmGameManager rhythmGameManager = RhythmGameManager.Instance;

            if (rhythmGameManager == null || BoardInstanceIsNull)
            {
                if (originalDamage.Count == 0 && currentCombo == 0 && SpawnedPets == 0) return;

                ResetData();
#if DEBUG
                if (DebugMode.Value)
                    DebugLogger.Msg("[Better Osu] Reset");
#endif
                return;
            }

            if (rhythmGameManager.CurrentTime == 0)
            {
                ResetData();
#if DEBUG
                if (DebugMode.Value)
                    DebugLogger.Msg("[Better Osu] Reset");
#endif
                return;

            }

            //MiniPet

            if (HelperPetSetting.Value)
            {
                if (SpawnedPets == 0)
                {
                    Vector2 centerWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Camera.main.nearClipPlane));
                    MiniPet pet = MiniPet.SetPet(board, centerWorldPos, (PetType)PetTypeSetting.SelectedValues.First());
                    SpawnedPets++;

                    DebugLogger.Msg("[Better Osu] Spawned Pet");
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
#if DEBUG
                    if (DebugMode.Value)
                        DebugLogger.Msg("[Better Osu] Removed Pet");
#endif
                }
            }

            // Bullet Damage
            currentCombo = rhythmGameManager.comboManager.currentCombo;
            DamageBuff = (float)currentCombo / instance.BulletsDamageIncreaseSetting.Value;


        }

        public override void OnDisable()
        {
            if (SpawnedPets > 0)
            {
                MiniPet pet = GameObject.FindObjectOfType<MiniPet>();
                UnityEngine.Object.Destroy(pet);
                pet.gameObject.SetActive(false);
                SpawnedPets--;
#if DEBUG
                if (DebugMode.Value)
                    DebugLogger.Msg("[Better Osu] Removed Pet");
#endif
            }

            ResetData();
#if DEBUG
            if (DebugMode.Value)
            DebugLogger.Msg("[Better Osu] Reset");
#endif
            
        }

        static Dictionary<Bullet, int> originalDamage = new Dictionary<Bullet, int>();

        [HarmonyPatch(typeof(Bullet))]
        public static class BulletPatch
        {

            [HarmonyPatch(nameof(Bullet.Update))]
            [HarmonyPrefix]
            public static void UpdatePatch(Bullet __instance)
            {
                if (BoardInstanceIsNull || instance == null) return;
                if (!instance.Active || !board.boardTag.rhythmGame) return;

                if (instance.BulletsDamageIncreaseSetting.Value <= instance.currentCombo)
                {

                    if (!originalDamage.ContainsKey(__instance))
                    {
                        originalDamage.Add(__instance, __instance.Damage);
                    }

                    if (instance.DamageBuff > 1 && __instance.Damage != (int)(originalDamage[__instance] * instance.DamageBuff))
                    {
                        __instance.Damage = (int)(originalDamage[__instance] * instance.DamageBuff);
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
                if (instance == null || BoardInstanceIsNull) return;
                if (!instance.Active || !board.boardTag.rhythmGame) return;
                if (!instance.RandomBulletSetting.Value || fromEnermy) return;

                // Ensure only change the cherry bullet
                if (theBulletType != BulletType.Bullet_superCherry) return;

                BulletType newType = theBulletType;

                if (instance.selectBulletsSetting.SelectedValues.Count == 1)
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.First();

                if (instance.selectBulletsSetting.SelectedValues.Count > 1)
                {
                    newType = (BulletType)instance.selectBulletsSetting.SelectedValues.ElementAt(
                        UnityEngine.Random.RandomRangeInt(0, instance.selectBulletsSetting.SelectedValues.Count));
                }
#if DEBUG
                if (instance.DebugMode.Value)
                    DebugLogger.Msg($"[Better Osu] Spawned Bullet: {newType.ToString()} ({(int)newType}");
#endif
                if (instance.selectBulletsSetting.IsSelected((int)newType))
                    theBulletType = newType;
            }
        }

        [HarmonyPatch(typeof(Board))]
        public static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Awake))]
            [HarmonyPostfix]
            public static void AwakePatch()
            {
                if (instance == null) return;
                instance.ResetData();
#if DEBUG
                if (instance.DebugMode.Value)
                    DebugLogger.Msg("[Better Osu] Reset");
#endif
            }

            [HarmonyPatch(nameof(Board.OnDestroy))]
            [HarmonyPostfix]
            public static void OnDestroyPatch()
            {
                if (instance == null) return;
                instance.ResetData();

#if DEBUG
                if (instance.DebugMode.Value)
                    DebugLogger.Msg("[Better Osu] Reset");
#endif
            }
        }
        

    }
}
