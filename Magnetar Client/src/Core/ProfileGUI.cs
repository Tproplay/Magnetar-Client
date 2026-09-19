using System;
using UnityEngine;
using Magnetar_Client.Utils;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;

namespace Magnetar_Client.Core
{
    public static class ProfileGUI
    {
        private const float BaseWindowWidth = 480f;
        private const float BaseWindowHeight = 420f;
        private const float BaseElementHeight = 30f;

        public static Rect WindowRect = new Rect(
            (Config.WindowWidth - Config.S(BaseWindowWidth)) / 2,
            (Config.WindowHeight - Config.S(BaseWindowHeight)) / 2,
            Config.S(BaseWindowWidth),
            Config.S(BaseWindowHeight));

        private static string newProfileInput = "";
        private static float scrollY = 0f;
        private static float elementHeight => Config.S(BaseElementHeight);

        public static void Render()
        {
            Event e = Event.current;

            // Handle Escape key
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Config.showgui = false;
                e.Use();
                return;
            }

            Config.RescaleAroundCenter(ref WindowRect, Config.S(BaseWindowWidth), Config.S(BaseWindowHeight));

            GUIStyle windowBgStyle = Magnetar_Default.SettingsWndowBgStyle ?? Magnetar_Default.CategoryWindowStyle;

            WindowRect = GUI.Window(
                4002,
                WindowRect,
                (GUI.WindowFunction)DrawProfileWindow,
                "",
                windowBgStyle
            );
        }

        private static void DrawProfileWindow(int windowID)
        {
            Event e = Event.current;
            float w = WindowRect.width;
            float indent = Config.S(12f);
            float y = Config.S(35f);

            // Header Banner
            Rect headerBgRect = new Rect(0, 0, w, y - indent);
            GUI.Box(headerBgRect, Translator.Translate("Profile Manager"), Magnetar_Default.SettingsWndowStyle);

            // Current Profile (Vertically Centered with SettingTextStyle)
            GUI.Label(
                new Rect(indent, y, w - (indent * 2), elementHeight),
                $"{Translator.Translate("Current Active Profile")}: <color=yellow>{Config.CurrentProfile}</color>",
                Magnetar_Default.SettingTextStyle
            );

            y += elementHeight + Config.S(10f);

            // --- CREATE NEW PROFILE ROW ---
            float labelW = Config.S(100f);
            float btnW = Config.S(90f);
            float gap = Config.S(8f);
            float inputW = w - (indent * 2f) - labelW - btnW - (gap * 2f);

            // 1. "New Profile:" Label aligned to MiddleLeft
            Rect labelRect = new Rect(indent, y, labelW, elementHeight);
            GUI.Label(labelRect, Translator.Translate("New Profile:"), Magnetar_Default.SettingTextStyle);

            // 2. Input Box aligned to MiddleLeft
            Rect inputRect = new Rect(indent + labelW + gap, y, inputW, elementHeight);
            newProfileInput = DrawSetting.DrawManualTextField(inputRect, newProfileInput, Translator.Translate("Enter profile name..."));

            // 3. Create Button
            Rect createBtnRect = new Rect(w - indent - btnW, y, btnW, elementHeight);
            bool isCreateHover = createBtnRect.Contains(e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0 && isCreateHover)
            {
                if (!string.IsNullOrWhiteSpace(newProfileInput))
                {
                    if (ProfileManager.CreateProfile(newProfileInput))
                    {
                        newProfileInput = "";
                        scrollY = 0f;
                    }
                }
                e.Use();
            }

            GUI.Box(createBtnRect, Translator.Translate("Create"), Magnetar_Default.SettingOff);

            y += elementHeight + Config.S(14f);

            // Separator Rule
            float lineThickness = Mathf.Max(1f, Config.S(1f));
            GUI.Box(new Rect(indent, y, w - (indent * 2f), lineThickness), "", Magnetar_Default.SeparatorStyle);
            y += lineThickness + Config.S(10f);

            // --- AVAILABLE PROFILES LIST ---
            GUI.Label(new Rect(indent, y, w - (indent * 2f), elementHeight), Translator.Translate("Available Profiles:"), Magnetar_Default.SettingTextStyle);
            y += elementHeight + Config.S(5f);

            float scrollAreaHeight = WindowRect.height - y - Config.S(15f);
            Rect scrollOuterRect = new Rect(indent, y, w - (indent * 2f), scrollAreaHeight);

            var profilesList = ProfileManager.Profiles;
            float rowSpacing = Config.S(6f);
            float contentHeight = profilesList.Count * (elementHeight + rowSpacing);
            float maxScroll = Mathf.Max(0f, contentHeight - scrollAreaHeight);

            if (scrollOuterRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                scrollY += e.delta.y * Config.S(20f);
                scrollY = Mathf.Clamp(scrollY, 0f, maxScroll);
                e.Use();
            }

            GUI.BeginGroup(scrollOuterRect);

            float itemY = -scrollY;
            for (int i = 0; i < profilesList.Count; i++)
            {
                string profileName = profilesList[i];
                bool isActive = string.Equals(Config.CurrentProfile, profileName, StringComparison.OrdinalIgnoreCase);
                bool isDefault = string.Equals(ProfileManager.DefaultProfile, profileName, StringComparison.OrdinalIgnoreCase);

                if (itemY + elementHeight >= 0 && itemY <= scrollAreaHeight)
                {
                    Rect itemRect = new Rect(0, itemY, scrollOuterRect.width, elementHeight);
                    float delBtnW = Config.S(65f);
                    float delBtnH = elementHeight - Config.S(6f);
                    Rect deleteBtnRect = new Rect(itemRect.width - delBtnW - Config.S(4f), itemY + Config.S(3f), delBtnW, delBtnH);

                    bool isItemHovered = itemRect.Contains(e.mousePosition);
                    bool isDeleteHovered = !isDefault && deleteBtnRect.Contains(e.mousePosition);

                    // Row background
                    GUIStyle rowStyle = isActive
                        ? Magnetar_Default.SettingOn
                        : (isItemHovered && !isDeleteHovered ? Magnetar_Default.SettingOff : Magnetar_Default.CategoryModuleOffStyle);

                    GUI.Box(itemRect, "", rowStyle);

                    // Row text: vertically centered via SettingTextStyle and full row elementHeight
                    string labelText = isActive
                        ? $"<b><color=yellow>{profileName}</color> ({Translator.Translate("Active")})</b>"
                        : profileName;

                    float textWidth = !isDefault ? itemRect.width - delBtnW - Config.S(20f) : itemRect.width - Config.S(20f);
                    Rect textRect = new Rect(Config.S(10f), itemY, textWidth, elementHeight);

                    GUI.Label(textRect, labelText, Magnetar_Default.SettingTextStyle);

                    // Delete Button
                    if (!isDefault)
                    {
                        Color oldBg = GUI.backgroundColor;
                        GUI.backgroundColor = isDeleteHovered ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.85f, 0.25f, 0.25f, 1f);
                        GUI.Box(deleteBtnRect, Translator.Translate("Delete"), Magnetar_Default.SettingOff);
                        GUI.backgroundColor = oldBg;
                    }

                    // Click Detection
                    if (e.type == EventType.MouseDown && e.button == 0 && isItemHovered)
                    {
                        if (isDeleteHovered)
                        {
                            ProfileManager.DeleteProfile(profileName);
                            e.Use();
                        }
                        else if (!isActive)
                        {
                            ProfileManager.SwitchProfile(profileName);
                            e.Use();
                        }
                    }
                }

                itemY += elementHeight + rowSpacing;
            }

            GUI.EndGroup();

            GUI.DragWindow(new Rect(0, 0, w, Config.S(25f)));

            if (WindowRect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
            {
                Input.ResetInputAxes();
                e.Use();
            }
        }
    }
}