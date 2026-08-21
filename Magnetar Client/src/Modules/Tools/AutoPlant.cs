using HarmonyLib;
using Magnetar_Client.Game;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class AutoPlant : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Auto Plant"; 
        public override string Description { get; set; } = "Allows you to place ghost version of " +
            "plants which will be replaced with the actual plants overtime.\nUse Right Click to delete Ghost Plants";
        public override string SearchHints { get; set; } = "autoplant ghostplant ghostplanting auto-plant" +
            " plantghost ghostspawn ghostautoplant ghostbuild ghostplacer ghostplacement ghost-plant" +
            " autoplanter ghostplantmod ghost-planting autofill ghostseeds ghostgrowth ghost-build " +
            "ghostingplant autoplantmod ghostauto ghosting ghostseeds ghosttimer ghost-spawn ghostplacer" +
            " ghostplacing ghostplantingtool";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static AutoPlant instance;

        public class GhostPlantRequest
        {
            public List<PlantType> targetPlantTypes = new List<PlantType>();
            public PlantType currentVisualType;
            public int column;
            public int row;
            public GameObject ghostVisual;

            public bool isProjectedSpread = false;
            public GhostPlantRequest parentSpread = null;
        }

        public List<GhostPlantRequest> PendingPlants = new List<GhostPlantRequest>();

        public BoolSetting RenderGhost;
        public FloatSetting GhostOpacity;
        public BoolSetting MultiPlant;
        public BoolSetting UseExtraPlant;
        public SelectSetting PlantingOrderSetting;

        public AutoPlant()
        {
            instance = this;

            CreateCategory("General");
            RenderGhost = new BoolSetting("Render Ghost Plant", true);
            GhostOpacity = new FloatSetting("Ghost Opacity", 0.1f, 1.0f, 0.6f, 2);
            MultiPlant = new BoolSetting("Multi Plant", false);
            UseExtraPlant = new BoolSetting("Use Extra Plant", true);
            PlantingOrderSetting = new SelectSetting("Planting Order", 0)
            {
                Options = new Dictionary<int, string>
                {
                    {0, "First" },
                    {1, "Last" },
                    {2, "Random" },
                }
            };

            AddSettings(RenderGhost, GhostOpacity, MultiPlant, UseExtraPlant, PlantingOrderSetting);
            EndCategory();
        }

        public override void OnLanguageChanged()
        {
            PlantingOrderSetting.CustomNames = PlantingOrderSetting.Options
                .ToDictionary(kvp => kvp.Key, kvp => Translator.Translate(kvp.Value));
        }

        // Mod Logic
        public override void OnDisable()
        {
            ClearGhostPlants();
        }

        public void ClearGhostPlants()
        {
            foreach (var req in PendingPlants)
            {
                if (req.ghostVisual != null)
                {
                    UnityEngine.Object.Destroy(req.ghostVisual);
                }
            }
            PendingPlants.Clear();
        }

        private bool HasRealPlant(int col, int row, PlantType type)
        {
            if (Board.Instance != null && GameData.plantList != null)
            {
                for (int i = 0; i < GameData.plantList.Count; i++)
                {
                    Plant p = GameData.plantList[i];
                    if (p != null && p.thePlantColumn == col && p.thePlantRow == row && p.thePlantHealth > 0 && p.thePlantType == type)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void OnUpdateActive()
        {
            if (PendingPlants.Count == 0 || Board.Instance == null || CreatePlant.Instance == null || InGameUI.Instance == null) return;

            if (Input.GetMouseButtonDown(1) && Mouse.Instance != null && Mouse.Instance.theItemOnMouse == null)
            {
                RemoveTopGhostAt(Mouse.Instance.theMouseColumn, Mouse.Instance.theMouseRow);
            }

            for (int i = PendingPlants.Count - 1; i >= 0; i--)
            {
                var req = PendingPlants[i];
                if (req.isProjectedSpread)
                {
                    if (req.parentSpread != null && !PendingPlants.Contains(req.parentSpread))
                    {
                        if (req.ghostVisual != null) UnityEngine.Object.Destroy(req.ghostVisual);
                        PendingPlants.RemoveAt(i);
                        continue;
                    }
                    if (HasRealPlant(req.column, req.row, PlantType.SmallPuff))
                    {
                        req.isProjectedSpread = false;
                        req.parentSpread = null;
                        if (req.targetPlantTypes.Count == 0)
                        {
                            if (req.ghostVisual != null) UnityEngine.Object.Destroy(req.ghostVisual);
                            PendingPlants.RemoveAt(i);
                        }
                        continue;
                    }
                }

                if (!req.isProjectedSpread && req.targetPlantTypes.Count == 0)
                {
                    if (req.ghostVisual != null) UnityEngine.Object.Destroy(req.ghostVisual);
                    PendingPlants.RemoveAt(i);
                }
            }

            if (PendingPlants.Count == 0) return;

            List<CardUI> availableCards = new List<CardUI>();
            foreach (CardUI card in InGameUI.Instance.Cards)
            {
                if (card != null && card.onCardBank)
                {
                    if (card.isExtra && !UseExtraPlant.Value) continue;
                    if (card.CD >= card.fullCD && AppData.board.theSun >= card.theSeedCost)
                    {
                        availableCards.Add(card);
                    }
                }
            }

            if (availableCards.Count == 0) return;

            Dictionary<Vector2Int, GhostPlantRequest> activeGhostsByTile = new Dictionary<Vector2Int, GhostPlantRequest>();
            foreach (var req in PendingPlants)
            {
                if (req.isProjectedSpread) continue;

                Vector2Int pos = new Vector2Int(req.column, req.row);
                if (!activeGhostsByTile.ContainsKey(pos))
                {
                    activeGhostsByTile[pos] = req;
                }
            }

            for (int i = availableCards.Count - 1; i >= 0; i--)
            {
                CardUI card = availableCards[i];

                if (AppData.board.theSun < card.theSeedCost) continue;

                List<GhostPlantRequest> candidates = new List<GhostPlantRequest>();
                foreach (var req in activeGhostsByTile.Values)
                {
                    if (req.targetPlantTypes.Count > 0 && req.targetPlantTypes[0] == card.thePlantType)
                    {
                        candidates.Add(req);
                    }
                }

                if (candidates.Count > 0)
                {
                    GhostPlantRequest chosen = null;

                    if (PlantingOrderSetting.Value == 0) chosen = candidates[0];
                    else if (PlantingOrderSetting.Value == 1) chosen = candidates[candidates.Count - 1];
                    else if (PlantingOrderSetting.Value == 2) chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];

                    Plant nativePlantResult = CreatePlant.Instance.SetPlant(chosen.column, chosen.row, card.thePlantType);

                    if (nativePlantResult != null)
                    {
                        card.CD = 0f;
                        card.isAvailable = false;
                        AppData.board.UseSun(card.theSeedCost);

                        chosen.targetPlantTypes.RemoveAt(0);

                        if (chosen.targetPlantTypes.Count == 0)
                        {
                            if (chosen.ghostVisual != null) UnityEngine.Object.Destroy(chosen.ghostVisual);
                            PendingPlants.Remove(chosen);
                            activeGhostsByTile.Remove(new Vector2Int(chosen.column, chosen.row));
                        }

                        availableCards.RemoveAt(i);
                    }
                }
            }
        }

        public bool RemoveTopGhostAt(int col, int row)
        {
            for (int i = PendingPlants.Count - 1; i >= 0; i--)
            {
                if (PendingPlants[i].column == col && PendingPlants[i].row == row)
                {
                    if (PendingPlants[i].ghostVisual != null)
                    {
                        UnityEngine.Object.Destroy(PendingPlants[i].ghostVisual);
                    }
                    PendingPlants.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private GameObject SpawnGhostVisual(PlantType plantType, Vector3 worldPosition)
        {
            if (!RenderGhost.Value) return null;

            GameObject originalPrefab = GameAPP.resourcesManager.plantPrefabs[plantType];
            if (originalPrefab == null) return null;

            GameObject ghost = UnityEngine.Object.Instantiate(originalPrefab, worldPosition, Quaternion.identity);

            var plantComp = ghost.GetComponent<Plant>();
            if (plantComp != null) UnityEngine.Object.Destroy(plantComp);

            var collider = ghost.GetComponent<Collider2D>();
            if (collider != null) UnityEngine.Object.Destroy(collider);

            var renderers = ghost.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                Color c = sr.color;
                c.a = GhostOpacity.Value;
                sr.color = c;
            }

            return ghost;
        }

        private int GetPuffCount(int col, int row)
        {
            int count = 0;
            if (!AppData.BoardInstanceIsNull && GameData.plantList != null)
            {
                for (int i = 0; i < GameData.plantList.Count; i++)
                {
                    Plant p = GameData.plantList[i];
                    if (p != null && p.thePlantColumn == col && p.thePlantRow == row && p.thePlantHealth > 0 && TypeMgr.IsPuff(p.thePlantType))
                    {
                        count++;
                    }
                }
            }
            foreach (var req in PendingPlants)
            {
                if (req.column == col && req.row == row)
                {
                    count += req.targetPlantTypes.Count(p => TypeMgr.IsPuff(p));
                    if (TypeMgr.IsPuff(req.currentVisualType) && req.targetPlantTypes.Count == 0 && req.isProjectedSpread)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private bool TryGetSpreadFusion(PlantType basePlant, PlantType topPlant, out PlantType resultType, out bool isFume)
        {
            resultType = basePlant;
            isFume = true;

            if (TypeMgr.IsPuff(basePlant))
            {
                string topName = topPlant.ToString();
                if (topName.Contains("FumeShroom") && System.Enum.TryParse("SpreadFume", out PlantType spreadFume))
                {
                    resultType = spreadFume;
                    isFume = true;
                    return true;
                }
                if (topName.Contains("ScaredyShroom") && System.Enum.TryParse("SpreadScaredyShroom", out PlantType spreadScaredy))
                {
                    resultType = spreadScaredy;
                    isFume = false;
                    return true;
                }
            }
            return false;
        }

        private void GenerateProjectedPuffs(int baseCol, int row, int count, bool isFume, GhostPlantRequest parent)
        {
            if (Mouse.Instance == null) return;

            for (int i = 1; i <= count; i++)
            {
                int targetCol = isFume ? baseCol + i : baseCol - i;
                if (targetCol < 0 || targetCol > 8) continue;

                float x = Mouse.Instance.GetBoxXFromColumn(targetCol);
                float y = Mouse.Instance.GetBoxYFromRow(row);
                Vector3 pos = new Vector3(x, y, 0f);

                GameObject visual = SpawnGhostVisual(PlantType.SmallPuff, pos);

                PendingPlants.Add(new GhostPlantRequest
                {
                    targetPlantTypes = new List<PlantType>(),
                    currentVisualType = PlantType.SmallPuff,
                    column = targetCol,
                    row = row,
                    ghostVisual = visual,
                    isProjectedSpread = true,
                    parentSpread = parent
                });
            }
        }

        private bool CanStack(PlantType basePlant, PlantType topPlant)
        {
            if (basePlant == topPlant)
            {
                if (TypeMgr.IsPuff(basePlant)) return true; 
                return false;
            }

            if (TypeMgr.IsPumpkin(topPlant) || TypeMgr.IsPumpkin(basePlant)) return true;
            if (TypeMgr.IsPot(basePlant)) return true;
            if (TypeMgr.IsLily(basePlant)) return true;

            string baseName = basePlant.ToString();
            string topName = topPlant.ToString();

            if (baseName.Contains("LilyPad")) return true;
            if (topName.Contains("CoffeeBean")) return true;

            if (TypeMgr.IsPuff(basePlant))
            {
                if (topName.Contains("FumeShroom") || topName.Contains("ScaredyShroom")) return true;
            }

            return false;
        }

        public bool QueueGhostPlant(PlantType plantType, int col, int row, Vector3 worldPosition)
        {
            if (TypeMgr.IsPuff(plantType))
            {
                if (GetPuffCount(col, row) >= 3) return false;
            }

            GhostPlantRequest topGhost = null;
            foreach (var req in PendingPlants)
            {
                if (req.column == col && req.row == row)
                {
                    topGhost = req;
                }
            }

            if (topGhost != null)
            {
                bool isSpread = TryGetSpreadFusion(topGhost.currentVisualType, plantType, out PlantType spreadType, out bool isFumeGhost);

                if (MixData.TryGetMix(topGhost.currentVisualType, plantType, out PlantType mixedType, false) || isSpread)
                {
                    PlantType finalType = isSpread ? spreadType : mixedType;
                    int puffsToSpread = isSpread ? GetPuffCount(col, row) : 0;

                    topGhost.targetPlantTypes.Add(plantType);
                    topGhost.currentVisualType = finalType;

                    if (topGhost.ghostVisual != null) UnityEngine.Object.Destroy(topGhost.ghostVisual);
                    topGhost.ghostVisual = SpawnGhostVisual(finalType, worldPosition);

                    if (isSpread)
                    {
                        GenerateProjectedPuffs(col, row, puffsToSpread, isFumeGhost, topGhost);
                    }
                    return true;
                }
                else
                {
                    if (CanStack(topGhost.currentVisualType, plantType))
                    {
                        GameObject newVisual = SpawnGhostVisual(plantType, worldPosition);

                        PendingPlants.Add(new GhostPlantRequest
                        {
                            targetPlantTypes = new List<PlantType> { plantType },
                            currentVisualType = plantType,
                            column = col,
                            row = row,
                            ghostVisual = newVisual
                        });
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            bool hasBasePlant = false;
            bool isLegalStack = true;

            if (!AppData.BoardInstanceIsNull && GameData.plantList != null)
            {
                for (int i = 0; i < GameData.plantList.Count; i++)
                {
                    Plant p = GameData.plantList[i];
                    if (p != null && p.thePlantColumn == col && p.thePlantRow == row && p.thePlantHealth > 0)
                    {
                        hasBasePlant = true;

                        bool isSpreadReal = TryGetSpreadFusion(p.thePlantType, plantType, out PlantType spreadTypeReal, out bool isFumeReal);

                        if (MixData.TryGetMix(p.thePlantType, plantType, out PlantType mixedRealType, false) || isSpreadReal)
                        {
                            PlantType finalRealType = isSpreadReal ? spreadTypeReal : mixedRealType;
                            int puffsToSpreadReal = isSpreadReal ? GetPuffCount(col, row) : 0;

                            GameObject mixVisual = SpawnGhostVisual(finalRealType, worldPosition);
                            GhostPlantRequest newReq = new GhostPlantRequest
                            {
                                targetPlantTypes = new List<PlantType> { plantType },
                                currentVisualType = finalRealType,
                                column = col,
                                row = row,
                                ghostVisual = mixVisual
                            };
                            PendingPlants.Add(newReq);

                            if (isSpreadReal)
                            {
                                GenerateProjectedPuffs(col, row, puffsToSpreadReal, isFumeReal, newReq);
                            }
                            return true;
                        }

                        if (!CanStack(p.thePlantType, plantType))
                        {
                            isLegalStack = false;
                        }
                    }
                }
            }
            if (hasBasePlant && !isLegalStack)
            {
                return false;
            }

            if (topGhost == null && !hasBasePlant)
            {
                bool isWaterTile = false;
                if (Board.Instance != null && Board.Instance.roadType != null && row >= 0 && row < Board.Instance.roadType.Length)
                {
                    string rt = Board.Instance.roadType[row].ToString().ToLower();
                    if (rt.Contains("water") || rt.Contains("pool") || rt.Contains("river"))
                    {
                        isWaterTile = true;
                    }
                }

                bool isWaterPlant = TypeMgr.IsWaterPlant(plantType) || TypeMgr.IsLily(plantType) || TypeMgr.IsTangkelp(plantType);
                bool isFlyingPlant = TypeMgr.FlyingPlants(plantType);

                if (isWaterTile && !isWaterPlant && !isFlyingPlant) return false;
                if (!isWaterTile && isWaterPlant) return false;
            }

            GameObject initialVisual = SpawnGhostVisual(plantType, worldPosition);

            PendingPlants.Add(new GhostPlantRequest
            {
                targetPlantTypes = new List<PlantType> { plantType },
                currentVisualType = plantType,
                column = col,
                row = row,
                ghostVisual = initialVisual
            });

            return true;
        }


        [HarmonyPatch(typeof(Mouse))]
        public static class AutoPlantUpdateBypassPatch
        {
            public struct UpdateState
            {
                public CardUI hoveredCard;
                public float originalCD;
                public int originalCost;
                public bool originalAvailable;
                public bool wasSpoofed;
            }

            [HarmonyPatch(nameof(Mouse.Update))]
            [HarmonyPrefix]
            public static void Prefix(out UpdateState __state)
            {
                __state = new UpdateState { wasSpoofed = false };

                if (instance == null || !instance.Active) return;

                if (GameAPP.theGameStatus == GameStatus.Selecting) return;
                if (!Input.GetMouseButtonDown(0)) return;

                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

                foreach (var hit in Physics2D.RaycastAll(mousePos2D, Vector2.zero))
                {
                    if (hit.collider != null)
                    {
                        var card = hit.collider.GetComponent<CardUI>();

                        if (card != null && card.onCardBank)
                        {
                            __state.hoveredCard = card;
                            __state.originalCD = card.CD;
                            __state.originalCost = card.theSeedCost;
                            __state.originalAvailable = card.isAvailable;
                            __state.wasSpoofed = true;

                            card.CD = card.fullCD;
                            card.theSeedCost = 0;
                            card.isAvailable = true;
                            break;
                        }
                    }
                }
            }

            [HarmonyPatch(nameof(Mouse.Update))]
            [HarmonyPostfix]
            public static void Postfix(UpdateState __state)
            {
                if (__state.wasSpoofed && __state.hoveredCard != null)
                {
                    __state.hoveredCard.CD = __state.originalCD;
                    __state.hoveredCard.theSeedCost = __state.originalCost;
                    __state.hoveredCard.isAvailable = __state.originalAvailable;
                }
            }
        }

        [HarmonyPatch(typeof(Mouse))]
        public static class AutoPlantPlacementPatch
        {
            [HarmonyPatch(nameof(Mouse.TryToSetPlantByCard))]
            [HarmonyPrefix]
            public static bool Prefix(Mouse __instance)
            {
                if (instance == null || !instance.Active) return true;

                CardUI heldCard = __instance.theCardOnMouse;
                if (heldCard == null || Board.Instance == null) return true;

                if (heldCard.CD > 0f || AppData.board.theSun < heldCard.theSeedCost)
                {
                    int col = __instance.theMouseColumn;
                    int row = __instance.theMouseRow;

                    if (col < 0 || row < 0) return true;

                    float x = __instance.GetBoxXFromColumn(col);
                    float y = __instance.GetBoxYFromRow(row);
                    Vector3 worldPos = new Vector3(x, y, 0f);

                    if (instance.QueueGhostPlant(heldCard.thePlantType, col, row, worldPos))
                    {
                        if (!instance.MultiPlant.Value)
                        {
                            if (__instance.theItemOnMouse != null)
                            {
                                UnityEngine.Object.Destroy(__instance.theItemOnMouse);
                            }
                            heldCard.isPickUp = false;
                            __instance.ClearItemOnMouse(false);
                        }
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Board))]
        public static class AutoPlantBoardCleanupPatch
        {
            [HarmonyPatch(nameof(Board.OnDestroy))]
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (instance != null)
                {
                    instance.ClearGhostPlants();
                }
            }
        }
    }
}