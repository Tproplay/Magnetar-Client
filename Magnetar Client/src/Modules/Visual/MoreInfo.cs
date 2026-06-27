using UnityEngine;
using HarmonyLib;
using Il2CppTMPro;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.Modules
{
    public class MoreInfo : Module
    {
        // Mod Info
        public override string Name { get; set; } = "More Info";
        public override string Description { get; set; } = "";
        public override string SearchHints { get; set; } = "fpslimit fpscap limitfps framespersecond fpsbreaker unlockfps capfps customfps " +
            "maxfps fpsfix fpsunlocked framecap framepersecond fpslimiter fpxlimit fpaslimit fps-limit fpsbypass bypassfps nofpslimit " +
            "unlimitedfps fpsset setfps fps-cap morefps fpsboost lagfix frameslimit";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;

        // Mod Data

        public static MoreInfo instance;


        public MoreInfo()
        {
            instance = this;

            CreateCategory("General");


            EndCategory();
        }

        // Mod Logic

        public override void OnGUI()
        {

        }

    }
}