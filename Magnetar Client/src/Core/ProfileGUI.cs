using System;
using UnityEngine;
using Magnetar_Client.Utils;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;

namespace Magnetar_Client.Core
{
    public static class ProfileGUI
    {
        // Base (unscaled, GUIScale == 1) sizes - actual sizes are derived via
        // Config.S() so this window scales with the rest of the UI.
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

            // Keep the window sized for the current GUIScale, growing or
            // shrinking around wherever it's currently positioned so it
            // doesn't jump if the scale changes while it's open.
            Config.RescaleAroundCenter(ref WindowRect, Config.S(BaseWindowWidth), Config.S(BaseWindowHeight));

            WindowRect = GUI.Window(
                4002,
                WindowRect,
                (GUI.WindowFunction)DrawProfileWindow,
                "",
                Magnetar_Default.ModuleWindow
            );
        }

        private static void DrawProfileWindow(int windowID)
        {
            Event e = Event.current;
            float w = WindowRect.width;
            float indent = Config.S(12f);
            float y = Config.S(35f);

            Rect headerBgRect = new Rect(0, 0, w, y - indent);
            GUI.Box(headerBgRect, Translator.Translate("Profile Manager"), Magnetar_Default.SettingsWindow);

            GUI.Label(
                new Rect(indent, y, w - (indent * 2), elementHeight),
                $"{Translator.Translate("Current Active Profile")}: <color=yellow>{Config.CurrentProfile}</color>",
                Magnetar_Default.SettingDescriptionStyle
            );

            y += elementHeight + Config.S(10f);

            // CREATE NEW PROFILE INPUT

            GUI.Label(new Rect(indent, y, Config.S(100f), elementHeight), Translator.Translate("New Profile:"), Magnetar_Default.SettingDescriptionStyle);

            Rect inputRect = new Rect(indent + Config.S(95f), y, w - Config.S(215f), elementHeight);
            newProfileInput = DrawSetting.DrawManualTextField(inputRect, newProfileInput, Translator.Translate("Enter profile name..."));

            Rect createBtnRect = new Rect(w - Config.S(110f), y, Config.S(98f), elementHeight);
            bool isCreateHover = createBtnRect.Contains(e.mousePosition);
            if (isCreateHover) GUI.backgroundColor = Magnetar_Default.AccentColor;

            // Manual click detection for Create button
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
            GUI.backgroundColor = Color.white;

            y += elementHeight + Config.S(15f);

            // Separator
            GUI.Box(new Rect(indent, y, w - (indent * 2), Config.S(1f)), "", Magnetar_Default.SettingsWindow);
            y += Config.S(10f);

            //  AVAILABLE PROFILES LIST

            GUI.Label(new Rect(indent, y, w - (indent * 2), elementHeight), Translator.Translate("Available Profiles:"), Magnetar_Default.SettingDescriptionStyle);
            y += elementHeight + Config.S(5f);

            float scrollAreaHeight = WindowRect.height - y - Config.S(15f);
            Rect scrollOuterRect = new Rect(indent, y, w - (indent * 2), scrollAreaHeight);

            var profilesList = ProfileManager.Profiles;
            float rowSpacing = Config.S(6f);
            float contentHeight = profilesList.Count * (elementHeight + rowSpacing);
            float maxScroll = Mathf.Max(0f, contentHeight - scrollAreaHeight);

            // Process mouse scroll wheel on the list area
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

                // View culling check
                if (itemY + elementHeight >= 0 && itemY <= scrollAreaHeight)
                {
                    Rect itemRect = new Rect(0, itemY, scrollOuterRect.width, elementHeight);
                    Rect deleteBtnRect = new Rect(itemRect.width - Config.S(75f), itemY + Config.S(3f), Config.S(70f), elementHeight - Config.S(6f));

                    bool isItemHovered = itemRect.Contains(e.mousePosition);
                    bool isDeleteHovered = !isDefault && deleteBtnRect.Contains(e.mousePosition);

                    // Entry bar highlight on hover/active
                    if (isActive)
                    {
                        GUI.Box(itemRect, "", Magnetar_Default.SettingsWindow);
                    }
                    else if (isItemHovered && !isDeleteHovered)
                    {
                        GUI.backgroundColor = Magnetar_Default.AccentColor;
                        GUI.Box(itemRect, "", Magnetar_Default.SettingOff);
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUI.Box(itemRect, "", Magnetar_Default.ModuleOff);
                    }

                    string labelText = isActive ? $"<b><color=yellow>{profileName}</color> ({Translator.Translate("Active")})</b>" : profileName;
                    float textWidth = !isDefault ? itemRect.width - Config.S(90f) : itemRect.width - Config.S(20f);
                    GUI.Label(new Rect(Config.S(10f), itemY + Config.S(3f), textWidth, elementHeight), labelText, Magnetar_Default.SettingDescriptionStyle);

                    // Delete Button
                    if (!isDefault)
                    {
                        GUI.backgroundColor = isDeleteHovered ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.85f, 0.25f, 0.25f, 1f);
                        GUI.Box(deleteBtnRect, Translator.Translate("Delete"), Magnetar_Default.SettingOff);
                        GUI.backgroundColor = Color.white;
                    }

                    // Manual Click Detection for the entire Profile Bar
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