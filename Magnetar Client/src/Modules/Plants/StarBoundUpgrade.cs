using System.Collections.Generic;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class StarBoundUpgrade : Module
    {
        // Mod Info
        public override string Name { get; set; } = "StarBound Upgrade";
        public override string Description { get; set; } = "Allows you to upgrade specific plants to their StarBound" +
            " version.";
        public override string SearchHints { get; set; } = "starboundupgrade starbound starupgrade starplant" +
            " plantupgrade starboundmode starboundconversion upgradeplant plant-starbound star-bound " +
            "upgrade-starbound starboundmod starboundplants starboundify starboundchanger plantstar " +
            "upgrade-to-starbound starversion starboundlevel starboundtool starboundlogic";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static StarBoundUpgrade instance;

        public BindSetting UpgradeKey;

        public StarBoundUpgrade()
        {
            instance = this;

            CreateCategory("General");

            UpgradeKey = new BindSetting("Upgrade Key", new List<KeyCode> { KeyCode.U });

            AddSettings(UpgradeKey);
            EndCategory();
        }

        // Mod Logic

        public override void OnUpdateActive()
        {
            if (GetKeyComboDown(UpgradeKey.BindKeys))
            {
                var cast = Physics2D.Raycast(Mouse.Instance.MousePosition, new Vector2(0, 0));

                if (cast.collider != null)
                {
                    Plant plant = cast.collider.gameObject.GetComponent<Plant>();
                    if (plant != null)
                    {
                        if (plant.OnStarUp()) plant.StarUp();
                    }
                }
            }
        }

    }
}
