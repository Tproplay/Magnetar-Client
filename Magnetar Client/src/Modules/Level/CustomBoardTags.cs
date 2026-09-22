using static Magnetar_Client.Game.AppData;
using HarmonyLib;
using System.Reflection;
using Newtonsoft.Json.Linq;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{

    public class CustomBoardTags : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Custom Board Tags";
        public override string Description { get; set; } = "Allows you to modify the BoardTag of the current level.";
        public override string SearchHints { get; set; } = "customboardtags boardtag boardtagger boardtagmod " +
            "leveltags boardtageditor boardtagmodifier customtags leveltagger boardtagchanger tagmanager " +
            "tageditor leveltagmod tagmodifier boardproperties tagoverride boardconfig levelproperties tagselector " +
            "boardtagsettings boardtagcheat";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        // Mod Data

        bool _active = false;
        public override bool Active 
        { 
            get => _active;
            set
            {
                _active = value;
                BoardTagSetting.IsDisabled = !value;
            }
        }

        public static CustomBoardTags instance;

        private static readonly FieldInfo[] BoardTagFields = typeof(Board.BoardTag).GetFields();

        public MultiSelectSetting BoardTagSetting;
        Board.BoardTag? _originalTag;
        public Board.BoardTag? OriginalTags
        {
            get => _originalTag;
            set
            {
                _originalTag = value;
                if (value == null)
                {
                    BoardTagSetting.SelectedValues.Clear();
                    BoardTagSetting.Options.Clear();
                    BoardTagSetting.AddOption(-1, "No Board Instance is active!");
                }
                else
                {
                    BoardTagSetting.SelectedValues.Clear();
                    BoardTagSetting.Options.Clear();

                    int i = 0;
                    foreach (FieldInfo field in BoardTagFields)
                    {
                        var name = field.Name;
                        var val = field.GetValue(value);

                        BoardTagSetting.AddOption(i, name);
                        if (val is bool select)
                        {
                            if (select) BoardTagSetting.SelectedValues.Add(i);
                        }
                        i++;
                    }
                }
            }
        }

        public CustomBoardTags() 
        { 
            instance = this;

            CreateCategory("General");

            BoardTagSetting = new("Tags")
            {
                Options = new System.Collections.Generic.Dictionary<int, string>
                {
                    {-1, "No Board Instance is active!"}
                },
                OnSelectionChanged = ApplyTagChange
            };

            AddSettings(BoardTagSetting);
            EndCategory();

        }


        // Mod Logic

        void ApplyTagChange(int tag, bool enable)
        {
            if (tag < 0 || tag >= BoardTagFields.Length || BoardInstanceIsNull) return;

            var boardTag = BoardInstance.boardTag;

            object boxedTag = boardTag;

            BoardTagFields[tag].SetValue(boxedTag, enable);

            BoardInstance.boardTag = (Board.BoardTag)boxedTag;
        }


        [HarmonyPatch(typeof(Board))]
        public static class BoardPatch
        {
            [HarmonyPatch(nameof(Board.Start))]
            [HarmonyPostfix]
            public static void StartPostfix(Board __instance)
            {
                if (instance == null) return;
                instance.OriginalTags = __instance.boardTag;
                instance.BoardTagSetting.IsDisabled = false;
            }

            [HarmonyPatch(nameof(Board.Die))]
            [HarmonyPostfix]
            public static void DiePostfix()
            {
                if (instance == null) return;
                instance.OriginalTags = null;
                instance.BoardTagSetting.IsDisabled = true;
            }
        }

    }

}
