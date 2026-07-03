using HarmonyLib;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.AppData;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class Control : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Control";
        public override string Description { get; set; } = "Allows you to control any plant using the WASD keys.\n" +
            "Press the <color=yellow>Control Key</color> to control the plant at mouse position.\n" +
            "Press the <color=yellow>Control Key</color> again to remove the plant from control.";
        public override string SearchHints { get; set; } = "control wasdplant wasdcontrol moveplant plantcontrol " +
            "plantmovement wasd controller plantwasd controlmod plantmover moveplants plantsteer wasdmove driveplant" +
            " pilotplant steerplant wasdmovement plantdrive plantpilot manualcontrol plantcontrolmod movementmod" +
            " movementhack wasdmode plantcontrolmode move";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static Control instance;

        public BindSetting ControlKey;

        public Control()
        {
            instance = this;

            CreateCategory("General");

            ControlKey = new BindSetting("Control Key", new List<KeyCode> { KeyCode.U});
            
            AddSettings(ControlKey);
            EndCategory();
        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (GetKeyComboDown(ControlKey.BindKeys))
            {
                var cast = Physics2D.Raycast(Mouse.Instance.MousePosition, new Vector2(0, 0));
                
                if (cast.collider != null)
                {
                    Plant plant = cast.collider.gameObject.GetComponent<Plant>();
                    if (plant!= null)
                    {
                        var current_plant = board.controledPlant;
                        if (board.controledPlant != plant) { board.controledPlant = plant; current_plant?.UpdateText(); }
                        else board.controledPlant = null;
                        plant.UpdateText();
                    }
                }
            }
        }

        public override void OnDisable()
        {
            board.controledPlant = null;
        }

    }
}
