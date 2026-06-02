using HarmonyLib;
using Il2CppRhythmGame;
using UnityEngine;

namespace Magnetar_Client.Modules
{
    public class AutoOsu: Module
    {
        // Mod Info
        public override string Name { get; set; } = "Auto Osu";
        public override string Description { get; set; } = "Automatically plays the Explode-O-su mode.";
        public override string SearchHints { get; set; } = "autoosu rhythmmaster explodeosu rhythmmaster explodosu " +
            "auto-osu osuauto autorythm rhythmbot osuexplode osux2 rhythmsync osusync rythmmaster rhythm-master " +
            "explode-o-su rhythmplayer osuhelper rhythmtrainer";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Misc;



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
            if (__instance == null || __instance.tracks == null) return;

            float currentTime = __instance.CurrentTime;

            for (int i = 0; i < __instance.tracks.Count; i++)
            {
                var track = __instance.tracks[i];
                if (track == null || track.activeNotes == null) continue;

                for (int j = track.activeNotes.Count - 1; j >= 0; j--)
                {
                    var note = track.activeNotes[j];
                    if (note == null) continue;

                    float targetTime = note.targetTime;

                    // --- 1. NORMAL / SKILL NOTES ---
                    if (note.noteType == NoteType.Normal || note.noteType == NoteType.Skill)
                    {
                        if (currentTime >= targetTime)
                        {
                            try
                            {
                                note.OnClick();
                                __instance.IsHoldKeyPressed(i);
                            }
                            catch { }
                        }
                    }
                    // --- 2. HOLD NOTES ---
                    else if (note.noteType == NoteType.Hold)
                    {
                        float endTime = targetTime + note.holdDuration;

                        if (currentTime >= targetTime && currentTime < endTime)
                        {
                            if (!note.isHolding)
                            {
                                note.OnHoldStart();
                                __instance.IsHoldKeyPressed(i);
                            }
                        }
                        else if (currentTime >= endTime)
                        {
                            if (note.isHolding)
                            {
                                note.OnHoldComplete();
                            }
                        }
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
                if ( instance==null || !instance.Active) return;
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

            [HarmonyPatch(nameof(RhythmGameManager.OnNoteMissed))]
            [HarmonyPrefix]
            public static bool IgnoreMisses(RhythmGameManager __instance, FallingNote note)
            {
                if (instance == null || !instance.Active) return true;

                __instance.Shoot(NoteJudgeSystem.JudgeResult.Perfect);

                if (note != null && note.gameObject != null)
                {
                    Object.Destroy(note.gameObject);
                }

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
            [HarmonyPatch(nameof(FallingNote.hasMissed),(MethodType.Setter))]
            public static void HasMissedPatch(ref bool __bool)
            {
                if (instance == null || !instance.Active) return;
                __bool = false;
            }


        }

    }
}
