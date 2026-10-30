using HarmonyLib;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2CppRhythmGame;
#elif BEPINEX || RELEASE_BEPINEX
using RhythmGame;
#endif

namespace Magnetar_Client.Modules;

public class AutoOsu : Module
{
    // Mod Info
    public override string Name { get; set; } = "Auto Osu";
    public override string Description { get; set; } = "Automatically plays the Explode-O-su mode.";
    public override string SearchHints { get; set; } = "autoosu rhythmmaster explodeosu rhythmmaster explodosu " +
        "auto-osu osuauto autorythm rhythmbot osuexplode osux2 rhythmsync osusync rythmmaster rhythm-master " +
        "explode-o-su rhythmplayer osuhelper rhythmtrainer";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    // Mod Data
    public static AutoOsu instance;

    public AutoOsu()
    {
        instance = this;
    }

    // Mod Logic
    public override void OnUpdateActive()
    {
        RhythmGameManager __instance = RhythmGameManager.Instance;
        if (__instance == null || __instance.tracks == null || !__instance.isPlaying || __instance.isPaused) return;

        float currentTime = __instance.CurrentTime;

        for (int i = 0; i < __instance.tracks.Count; i++)
        {
            var track = __instance.tracks[i];
            if (track == null || track.activeNotes == null) continue;

            for (int j = track.activeNotes.Count - 1; j >= 0; j--)
            {
                try
                {
                    var note = track.activeNotes[j];
                    if (note == null || note.gameObject == null)
                    {
                        track.activeNotes.RemoveAt(j);
                        continue;
                    }

                    float targetTime = note.targetTime;

                    // --- 1. NORMAL / SKILL NOTES ---
                    if (note.noteType == NoteType.Normal || note.noteType == NoteType.Skill)
                    {
                        if (note.hasAutoPlayed) continue;

                        // Hit slightly early (within 50ms) to ensure frame drops never cause a miss
                        if (currentTime >= targetTime - 0.05f)
                        {
                            if (note.IsClickable())
                            {
                                note.hasAutoPlayed = true;
                                note.OnClick();
                            }
                        }
                    }
                    // --- 2. HOLD NOTES ---
                    else if (note.noteType == NoteType.Hold)
                    {
                        float endTime = targetTime + note.holdDuration;

                        // Start hold
                        if (currentTime >= targetTime - 0.05f && currentTime < endTime)
                        {
                            if (!note.isHolding)
                            {
                                if (note.IsClickable())
                                {
                                    note.OnClick();
                                }
                                note.OnHoldStart();
                            }
                        }
                        // End hold
                        else if (currentTime >= endTime)
                        {
                            if (note.isHolding)
                            {
                                try
                                {
                                    note.OnHoldComplete();
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch
                {
                    // Catch individual note errors to prevent one faulty note from halting the entire loop
                }
            }
        }
    }

    [HarmonyPatch(typeof(RhythmGameManager))]
    public static class RhythmGameManagerOverride
    {
        [HarmonyPatch(nameof(RhythmGameManager.Shoot))]
        [HarmonyPrefix]
        public static void ForceShootPerfect(ref NoteJudgeSystem.JudgeResult result)
        {
            if (instance == null || !instance.Active) return;
            result = NoteJudgeSystem.JudgeResult.Perfect;
        }

        [HarmonyPatch(nameof(RhythmGameManager.OnNoteClicked))]
        [HarmonyPrefix]
        public static void PerfectClickTiming(FallingNote note, ref float clickTime)
        {
            if (instance == null || !instance.Active) return;
            if (note != null)
            {
                clickTime = note.targetTime;
            }
        }

        [HarmonyPatch(nameof(RhythmGameManager.IsHoldKeyPressed))]
        [HarmonyPrefix]
        public static bool ForceHoldKeyPressed(ref bool __result)
        {
            if (instance == null || !instance.Active) return true;
            __result = true;
            return false;
        }

        [HarmonyPatch(nameof(RhythmGameManager.OnHoldNoteFailed))]
        [HarmonyPrefix]
        public static bool PreventHoldNoteFailed()
        {
            if (instance == null || !instance.Active) return true;
            return false;
        }

        [HarmonyPatch(nameof(RhythmGameManager.OnNoteMissed))]
        [HarmonyPrefix]
        public static bool IgnoreMisses(RhythmGameManager __instance, FallingNote note)
        {
            if (instance == null || !instance.Active) return true;

            try
            {
                if (__instance != null)
                {
                    __instance.Shoot(NoteJudgeSystem.JudgeResult.Perfect);

                    if (note != null)
                    {
                        int trackIdx = note.trackIndex;
                        if (__instance.tracks != null && trackIdx >= 0 && trackIdx < __instance.tracks.Count)
                        {
                            var track = __instance.tracks[trackIdx];
                            if (track != null)
                            {
                                track.RemoveNote(note);
                                if (track.activeNotes != null)
                                {
                                    track.activeNotes.Remove(note);
                                }
                            }
                        }

                        if (note.gameObject != null)
                        {
                            Object.Destroy(note.gameObject);
                        }
                    }
                }
            }
            catch { }

            return false; 
        }
    }

    [HarmonyPatch(typeof(NoteJudgeSystem))]
    public static class NoteJudgeSystemOverride
    {
        [HarmonyPatch(nameof(NoteJudgeSystem.Judge))]
        [HarmonyPrefix]
        public static bool ForcePerfectEnum(ref NoteJudgeSystem.JudgeResult __result)
        {
            if (instance == null || !instance.Active) return true;
            __result = NoteJudgeSystem.JudgeResult.Perfect;
            return false;
        }
    }

    [HarmonyPatch(typeof(FallingNote))]
    public static class FallingNotePatch
    {
        [HarmonyPatch(nameof(FallingNote.OnMiss))]
        [HarmonyPrefix]
        public static bool PreventFallingNoteMiss(FallingNote __instance)
        {
            if (instance == null || !instance.Active) return true;

            try
            {
                if (__instance != null && __instance.IsClickable())
                {
                    __instance.OnClick();
                }
            }
            catch { }

            return false;
        }
    }

    [HarmonyPatch(typeof(ComboManager))]
    public static class ComboManagerOverride
    {
        [HarmonyPatch(nameof(ComboManager.ResetCombo))]
        [HarmonyPrefix]
        public static bool PreventResetCombo()
        {
            if (instance == null || !instance.Active) return true;
            return false;
        }

        [HarmonyPatch(nameof(ComboManager.AddCombo))]
        [HarmonyPrefix]
        public static void ForcePerfectCombo(ref NoteJudgeSystem.JudgeResult result)
        {
            if (instance == null || !instance.Active) return;
            result = NoteJudgeSystem.JudgeResult.Perfect;
        }
    }
}
