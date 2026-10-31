using HarmonyLib;
using Il2CppInterop.Runtime;
using Magnetar_Client;
using Magnetar_Client.Modules;
using Magnetar_Client.Utils;
using Magnetar_Client.UI.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

#if MELONLOADER || BEPINEX
using System.IO;
using System.Threading.Tasks;
#endif

#if MELONLOADER || MELON_LOCAL
using Il2Cpp;
using Il2CppGameLevel.RogueShooting;
#elif BEPINEX || BEPINEX_LOCAL
using GameLevel.RogueShooting;
#endif

namespace MagnetarAIAddon;

public class AssistBot : Module
{
    // Mod info
    public override string Name { get; set; } = "Assist Bot";
    public override string Description { get; set; } = "Uses ML to suggest the best strategy for your game.";
    public override string SearchHints { get; set; } = "aiplay aimode autoplay bot machinelearning mlplay" +
        " aibotplay autonomousplay aiassist aidirector aiadvisor strategizer smartplay recommendations " +
        "aihelp mladvisor tacticalai aihints strategyhelper aiassistant optimalplay smartsuggestions" +
        " aicoach decisionmaker tactician aipredict advicebot aistrategy advisory";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Addon;
    public override bool defaultActive { get; } = true;
    public override bool EnableInVanillaMode { get; set; } = true;
    public override bool Active { get; set; } = true;
    public static AssistBot instance;

    // Mod data
#if MELONLOADER || BEPINEX
    public BoolSetting SendFeedback;
    public BoolSetting UpdateOnStart;

    private bool _wasShootingManagerActive = false;
    private float _lastStartRealtime = -10f;
#endif
    public BoolSetting SuggestBuffs;
    public BoolSetting AutoPickBestBuff;
    public FloatSetting AutoPickTimer;

    

    public AssistBot()
    {
        instance = this;

#if MELONLOADER || BEPINEX
        CreateCategory("General");

        SendFeedback = new BoolSetting("Send feedback", true);
        UpdateOnStart = new BoolSetting("Update on start", true);

        AddSettings(SendFeedback,UpdateOnStart);
        EndCategory();
#endif

        CreateCategory("The Gods Evolved");

        SuggestBuffs = new BoolSetting("Suggest buffs", true);
        AutoPickBestBuff = new BoolSetting("Auto pick best buff", false);
        AutoPickTimer = new FloatSetting("Auto pick delay", 0, 5, 1, 3, 0);

        AddSettings(SuggestBuffs, AutoPickBestBuff, AutoPickTimer);
        EndCategory();

        AIService.Init();
    }

    // Mod logic

#if MELONLOADER || BEPINEX
    public override void OnUpdate()
    {
        if (!Active) return;

        bool isAlive = ShootingManager.Instance != null;

        if (isAlive && !_wasShootingManagerActive)
        {
            _wasShootingManagerActive = true;
        }
        // This condition runs right after the ShootingManager.Instance becomes null
        else if (!isAlive && _wasShootingManagerActive)
        {
            _wasShootingManagerActive = false;

            // If its just a level restart, don't start a new session
            if (AIService.IsRestarting)
            {
                return;
            }

            // Runs only when we exit out of the level
            HandleSessionEnd();
        }
    }

    public void HandleSessionEnd()
    {
        // Don't send data if Send feedback is disabled
        if (!SendFeedback.Value)
        {
            Magnetar_Logger.DebugLogger.Warning("[Assist Bot] 'Send feedback' is disabled. Discarding session file.");
            AIService.DiscardCurrentSession();
            return;
        }

        // Send the data
        if (!AIService.IsValidCurrentSessionData())
        {
            AIService.DiscardCurrentSession();
            return;
        }

        string sessionPath = AIService.CurrentSessionFilePath;
        Magnetar_Logger.DebugLogger.Msg("[Assist Bot] Match ended. Uploading session telemetry to server...");

        _ = Task.Run(async () =>
        {
            bool uploaded = await AIService.UploadSessionTelemetryAsync(sessionPath);
            if (uploaded)
            {
                try { File.Delete(sessionPath); } catch { }
                Magnetar_Logger.DebugLogger.Msg("[Assist Bot] Session telemetry successfully sent to the server.");
            }
        });
    }
#endif
    [HarmonyPatch(typeof(ShootingManager))]
    public static class ShootingMgrPatch
    {
#if MELONLOADER || BEPINEX
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ShootingManager.Start))]
        public static void StartPostfix()
        {
            if (instance == null || !instance.Active) return;

            // Minimum 1s of gap to avoid double calling
            if (Time.realtimeSinceStartup - instance._lastStartRealtime < 1.0f)
                return;
            instance._lastStartRealtime = Time.realtimeSinceStartup;

            // If is restarting, don't start a new session 
            if (AIService.IsRestarting)
            {
                AIService.IsRestarting = false;
                AIService.ResetModifiersCounts();
                return;
            }

            // Start a new session if the setting is on
            if (instance.SendFeedback != null && instance.SendFeedback.Value)
            {
                AIService.StartNewSession();
                Magnetar_Logger.DebugLogger.Msg("[Assist Bot] Created new session.");
            }
        }
#endif

        private static float _lastBuffShowTime = -10f;
        private static int _lastProcessedMenuHash = 0;
        private static ICoroutineHandle _autoPickCoroutine = null;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ShootingManager.ShowBuff))]
        public static void ShowBuffPostfix(ShootingManager __instance)
        {
            if (instance == null || !instance.Active) return;

            var menu = GameObject.FindObjectOfType<MultipleChoiceMenu>();
            if (menu == null || menu.optionDatas == null || menu.optionDatas.Count == 0) return;

            // De-duplicate rapid trigger calls
            int currentMenuHash = menu.GetHashCode() ^ menu.optionDatas.Count;
            if (Time.realtimeSinceStartup - _lastBuffShowTime < 0.5f && _lastProcessedMenuHash == currentMenuHash)
                return;

            _lastBuffShowTime = Time.realtimeSinceStartup;
            _lastProcessedMenuHash = currentMenuHash;

            CancelActiveAutoPick();

            // 1. Snapshot board state
            var ownedPlantIds = ExtractOwnedPlantIds(__instance);
            string ownedPlantsStr = ownedPlantIds.Count > 0 ? string.Join(" ", ownedPlantIds) : "0";

            // 2. Evaluate candidate options with competitor context    
            var evaluatedCandidates = BuildAndScoreCandidates(menu, __instance, ownedPlantsStr);
            if (evaluatedCandidates.Count == 0) return;

            // 3. Render UI badges & translated texts
            ApplyOptionLabels(evaluatedCandidates);
            ApplyWindowTranslations(menu);

            // 4. Wrap click actions for single-execution safety & telemetry
            var decisionGuard = new ChoiceLatch();
            AttachChoiceCallbacks(evaluatedCandidates, decisionGuard);

            // 5. Trigger auto-picker if enabled
            TryTriggerAutoPick(menu, evaluatedCandidates, decisionGuard);
        }

        // -------------------------------------------------------------------------
        // Modular Subroutines
        // -------------------------------------------------------------------------

        /// <summary>
        /// Extracts the set of all plants the player owns
        /// </summary>
        private static HashSet<int> ExtractOwnedPlantIds(ShootingManager shootingMgr)
        {
            var idSet = new HashSet<int>();
            var targetPlants = shootingMgr.YourPlants != null && shootingMgr.YourPlants.Count > 0
                ? shootingMgr.YourPlants
                : shootingMgr.CurrentPlants;

            if (targetPlants != null)
            {
                for (int i = 0; i < targetPlants.Count; i++)
                {
                    idSet.Add((int)targetPlants[i]);
                }
            }

            return idSet;
        }
        
        /// <summary>
        /// Formats the option to be feededto the algorithm
        /// </summary>
        private static List<EvaluatedOption> BuildAndScoreCandidates(
            MultipleChoiceMenu menu,
            ShootingManager shootingMgr,
            string ownedPlantsStr)
        {
            // 1. Filter out null options
            var validOptions = new List<MultipleChoiceMenu.OptionData>();
            for (int i = 0; i < menu.optionDatas.Count; i++)
            {
                var opt = menu.optionDatas[i];
                if (opt != null)
                {
                    validOptions.Add(opt);
                }
            }

            int totalOptions = validOptions.Count;
            if (totalOptions == 0)
            {
                return new List<EvaluatedOption>();
            }

            // 2. Pre-sum total quality across all options
            float totalPoolQuality = 0f;
            for (int i = 0; i < totalOptions; i++)
            {
                totalPoolQuality += (float)validOptions[i].quality;
            }

            bool needsInference = (instance.SuggestBuffs != null && instance.SuggestBuffs.Value) ||
                                  (instance.AutoPickBestBuff != null && instance.AutoPickBestBuff.Value);

            var candidates = new List<EvaluatedOption>(totalOptions);

            // 3. Evaluate each option with true rival context
            for (int i = 0; i < totalOptions; i++)
            {
                var currentOpt = validOptions[i];
                float currentQuality = (float)currentOpt.quality;

                // Determine the highest quality among rival options
                float rivalMaxQuality = 0f;
                for (int j = 0; j < totalOptions; j++)
                {
                    if (i == j) continue; // Skip self

                    float rivalQ = (float)validOptions[j].quality;
                    if (rivalQ > rivalMaxQuality)
                    {
                        rivalMaxQuality = rivalQ;
                    }
                }

                // Determine average quality of rival options
                float rivalAvgQuality = totalOptions > 1
                    ? (totalPoolQuality - currentQuality) / (totalOptions - 1)
                    : 0f;

                string cleanTitle = Regex.Replace(currentOpt.title ?? "", @"\s*<color=.*?>.*?</color>\s*", "").Trim();
                int currentModifierCount = AIService.GetModifierPickCount(cleanTitle);

                var rec = new BuffDecisionData
                {
                    Difficulty = shootingMgr.difficulty,
                    CurrentPlantCount = shootingMgr.currentPlantCount,
                    MaxPlantCount = shootingMgr.maxPlantCount,
                    OwnedPlants = ownedPlantsStr,
                    PlantTypeId = (float)(int)currentOpt.thePlantType,
                    Quality = currentQuality,
                    TimesModifierPicked = (float)currentModifierCount,
                    CandidateTitle = cleanTitle,
                    PoolSize = (float)totalOptions,
                    CompetitorMaxQuality = rivalMaxQuality,
                    CompetitorAvgQuality = rivalAvgQuality,
                    Wave = RunProgressionTracker.GetContinuousWave(),
                    ZombieLoadout = (float)(int)ShootingManager.randomType
                };

                float rawScore = needsInference ? AIService.PredictBuffScore(rec) : 0f;

                candidates.Add(new EvaluatedOption
                {
                    Option = currentOpt,
                    Record = rec,
                    RawTitle = cleanTitle,
                    Score = rawScore
                });
            }

            return candidates;
        }

        private static void ApplyOptionLabels(List<EvaluatedOption> candidates)
        {
            float sumScores = candidates.Sum(x => x.Score);
            if (sumScores <= 0f) sumScores = 1f;

            foreach (var item in candidates)
            {
                string displayTitle = TranslationHelper.GetTranslatedTitle(item.RawTitle);
                if (string.IsNullOrEmpty(displayTitle))
                    displayTitle = item.RawTitle;

                if (instance.SuggestBuffs != null && instance.SuggestBuffs.Value)
                {
                    float normalized = Mathf.Clamp01(item.Score / sumScores);
                    float percentage = normalized * 100f;

                    Color lerpedColor = Color.Lerp(Color.red, Color.green, normalized);
                    byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(lerpedColor.r * 255f), 0, 255);
                    byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(lerpedColor.g * 255f), 0, 255);
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(lerpedColor.b * 255f), 0, 255);
                    string colorHex = $"{r:X2}{g:X2}{b:X2}";

                    item.Option.title = $"{displayTitle} <color=#{colorHex}>[{percentage:0.#}%]</color>";
                }
                else
                {
                    item.Option.title = displayTitle;
                }
            }
        }

        /// <summary>
        /// Manually translates the window using the translator mode
        /// </summary>
        /// <param name="menu"></param>
        private static void ApplyWindowTranslations(MultipleChoiceMenu menu)
        {
            var font = TranslationHelper.TranslatorFont;
            if (menu.windows == null) return;

            foreach (var window in menu.windows)
            {
                if (window == null) continue;

                if (font != null && window.title != null)
                    window.title.font = font;

                if (window.introduce != null)
                {
                    if (font != null) window.introduce.font = font;
                    string translatedBody = TranslationHelper.GetTranslatedTitle(window.introduce.text);
                    if (!string.IsNullOrEmpty(translatedBody))
                    {
                        window.introduce.text = translatedBody;
                    }
                }
            }
        }

        /// <summary>
        /// Modifies the options callback to note down which option was picked by the player
        /// </summary>
        private static void AttachChoiceCallbacks(List<EvaluatedOption> candidates, ChoiceLatch decisionGuard)
        {
            var allRecords = candidates.Select(c => c.Record).ToList();

            for (int i = 0; i < candidates.Count; i++)
            {
                int capturedIndex = i;
                var candidate = candidates[i];
                var originalCall = candidate.Option.call;

                Action wrappedAction = () =>
                {
                    try
                    {
                        if (decisionGuard.TryAcquire())
                        {
                            CancelActiveAutoPick();

#if MELONLOADER || BEPINEX
                            if (instance.SendFeedback != null && instance.SendFeedback.Value)
                            {
                                AIService.RecordSession(allRecords, capturedIndex);
                            }
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Failed to record choice: {ex}");
                    }

                    originalCall?.Invoke();
                };

                candidate.Option.call = DelegateSupport.ConvertDelegate<UnityAction>(wrappedAction);
            }
        }

        /// <summary>
        /// Starts a Coroutines that picks the option after a interval
        /// </summary>
        private static void TryTriggerAutoPick(MultipleChoiceMenu menu, List<EvaluatedOption> candidates, ChoiceLatch decisionGuard)
        {
            if (instance.AutoPickBestBuff == null || !instance.AutoPickBestBuff.Value) return;

            var bestCandidate = candidates.OrderByDescending(c => c.Score).FirstOrDefault();
            if (bestCandidate == null) return;

            float delay = instance.AutoPickTimer != null ? Mathf.Max(0f, instance.AutoPickTimer.Value) : 1f;

            _autoPickCoroutine = CoroutineManager.Start(AutoPickRoutine(menu, bestCandidate.Option, delay, decisionGuard));

        }

        private static void CancelActiveAutoPick()
        {
            if (_autoPickCoroutine != null)
            {
                CoroutineManager.Stop(_autoPickCoroutine);
                _autoPickCoroutine = null;
            }
        }

        private static System.Collections.IEnumerator AutoPickRoutine(
            MultipleChoiceMenu menu,
            MultipleChoiceMenu.OptionData targetOption,
            float delay,
            ChoiceLatch guard)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            if (guard == null || guard.IsHandled)
            {
                _autoPickCoroutine = null;
                yield break;
            }

            try
            {
                if (menu != null && targetOption != null)
                {
                    targetOption.call?.Invoke();
                    menu.OnSelect();
                }
            }
            catch (Exception ex)
            {
                Magnetar_Logger.DebugLogger.Error($"[Assist Bot] Auto-pick failed: {ex}");
            }

            _autoPickCoroutine = null;
        }

        // -------------------------------------------------------------------------
        // Supporting Types
        // -------------------------------------------------------------------------

        private class EvaluatedOption
        {
            public MultipleChoiceMenu.OptionData Option { get; set; }
            public BuffDecisionData Record { get; set; }
            public string RawTitle { get; set; }
            public float Score { get; set; }
        }

        private class ChoiceLatch
        {
            private int _handled = 0;
            public bool TryAcquire() => System.Threading.Interlocked.Exchange(ref _handled, 1) == 0;
            public bool IsHandled => System.Threading.Volatile.Read(ref _handled) == 1;
        }
    }

#if MELONLOADER || BEPINEX
    [HarmonyPatch(typeof(PauseMenu_Btn))]
    public static class PauseMenuPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PauseMenu_Btn.Restart))]
        public static void RestartPostfix()
        {
            if (instance == null || !instance.Active) return;

            AIService.IsRestarting = true;
        }
    }
        
#endif
}