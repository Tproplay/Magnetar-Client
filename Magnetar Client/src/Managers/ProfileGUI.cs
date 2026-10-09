using Magnetar_Client.Api;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using UnityEngine;

namespace Magnetar_Client.Core;

public static class ProfileGUI
{
    private const float BaseWindowWidth = 480f;
    private const float BaseWindowHeight = 420f;
    private const float BaseElementHeight = 30f;

    // 1. Dedicated domain for Profile GUI
    public static readonly TranslationDomain Domain = Translator.CreateDomain("ProfileGUI");

    // Shorthand scoped translation helper
    private static string TranslateText(string text) => Domain.Translate(text);

    public static Rect WindowRect = new(
        (Config.NativeWidth - Config.S(BaseWindowWidth)) / 2,
        (Config.NativeHeight - Config.S(BaseWindowHeight)) / 2,
        Config.S(BaseWindowWidth),
        Config.S(BaseWindowHeight));

    private static string newProfileInput = "";
    private static float scrollY;
    private static float elementHeight => Config.S(BaseElementHeight);

    public static void Init()
    {
        // 2. Hook English template generation
        Domain.OnDumpEnglishTemplate += DumpEnglishTemplates;

        ServiceRegistry.Register(new ProfileGUIService());

        SafeToCloseManager.RegisterGuard(() =>
        {
            if (Config.CurrentTab == TabType.PROFILE)
            {
                return DrawSetting.ActiveTextFieldId == -1
                       && DrawSetting.FocusedControlId == -1;
            }
            return true;
        });
    }

    private static void DumpEnglishTemplates(string englishDir)
    {
        string[] templateKeys = new[]
        {
            "Profile Manager",
            "Current Active Profile",
            "New Profile:",
            "Enter profile name...",
            "Create",
            "Available Profiles:",
            "Active",
            "Delete"
        };

        var templateDict = Translator.CreateDictionary(templateKeys);
        Translator.SaveJson(englishDir, "profile.json", templateDict);
    }

    public static void Render()
    {
        Config.RescaleAroundCenter(ref WindowRect, Config.S(BaseWindowWidth), Config.S(BaseWindowHeight));

        GUIStyle windowBgStyle = ThemeManager.SettingsWndowBgStyle ?? ThemeManager.CategoryWindowStyle;

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
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;
        float currentAlpha = AnimationHandler.CurrentEasedAlpha;

        GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, prevColor.a * currentAlpha);
        GUI.contentColor = new Color(prevContentColor.r, prevContentColor.g, prevContentColor.b, prevContentColor.a * currentAlpha);

        try
        {
            Event e = Event.current;
            float w = WindowRect.width;
            float indent = Config.S(12f);
            float y = Config.S(35f);

            Rect headerBgRect = new(0, 0, w, y - indent);
            GUI.Box(headerBgRect, TranslateText("Profile Manager"), ThemeManager.SettingsWndowStyle);

            GUI.Label(
                new Rect(indent, y, w - (indent * 2), elementHeight),
                $"{TranslateText("Current Active Profile")}: <color=yellow>{Config.CurrentProfile}</color>",
                ThemeManager.SettingTextStyle
            );

            y += elementHeight + Config.S(10f);

            // --- CREATE NEW PROFILE ROW ---
            float labelW = Config.S(100f);
            float btnW = Config.S(90f);
            float gap = Config.S(8f);
            float inputW = w - (indent * 2f) - labelW - btnW - (gap * 2f);

            Rect labelRect = new(indent, y, labelW, elementHeight);
            GUI.Label(labelRect, TranslateText("New Profile:"), ThemeManager.SettingTextStyle);

            Rect inputRect = new(indent + labelW + gap, y, inputW, elementHeight);
            newProfileInput = DrawSetting.DrawManualTextField(inputRect, newProfileInput, TranslateText("Enter profile name..."));

            Rect createBtnRect = new(w - indent - btnW, y, btnW, elementHeight);
            bool isCreateHover = createBtnRect.Contains(e.mousePosition);

            Color prevBg = GUI.backgroundColor;
            if (isCreateHover) GUI.backgroundColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
            GUI.Box(createBtnRect, TranslateText("Create"), ThemeManager.SettingOff);
            GUI.backgroundColor = prevBg;

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

            y += elementHeight + Config.S(14f);

            float lineThickness = Mathf.Max(1f, Config.S(1f));
            GUI.Box(new Rect(indent, y, w - (indent * 2f), lineThickness), "", ThemeManager.SeparatorStyle);
            y += lineThickness + Config.S(10f);

            // --- AVAILABLE PROFILES LIST ---
            GUI.Label(new Rect(indent, y, w - (indent * 2f), elementHeight), TranslateText("Available Profiles:"), ThemeManager.SettingTextStyle);
            y += elementHeight + Config.S(5f);

            float scrollAreaHeight = WindowRect.height - y - Config.S(15f);
            Rect scrollOuterRect = new(indent, y, w - (indent * 2f), scrollAreaHeight);

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
                bool isDefault = string.Equals(Config.DefaultProfile, profileName, StringComparison.OrdinalIgnoreCase);

                if (itemY + elementHeight >= 0 && itemY <= scrollAreaHeight)
                {
                    Rect itemRect = new(0, itemY, scrollOuterRect.width, elementHeight);
                    float delBtnW = Config.S(65f);
                    float delBtnH = elementHeight - Config.S(6f);
                    Rect deleteBtnRect = new(itemRect.width - delBtnW - Config.S(4f), itemY + Config.S(3f), delBtnW, delBtnH);

                    bool isItemHovered = itemRect.Contains(e.mousePosition);
                    bool isDeleteHovered = !isDefault && deleteBtnRect.Contains(e.mousePosition);

                    GUIStyle rowStyle = isActive
                        ? ThemeManager.SettingOn
                        : (isItemHovered && !isDeleteHovered ? ThemeManager.SettingOff : ThemeManager.CategoryModuleOffStyle);

                    GUI.Box(itemRect, "", rowStyle);

                    string labelText = isActive
                        ? $"<b><color=yellow>{profileName}</color> ({TranslateText("Active")})</b>"
                        : profileName;

                    float textWidth = !isDefault ? itemRect.width - delBtnW - Config.S(20f) : itemRect.width - Config.S(20f);
                    Rect textRect = new(Config.S(10f), itemY, textWidth, elementHeight);

                    GUI.Label(textRect, labelText, ThemeManager.SettingTextStyle);

                    if (!isDefault)
                    {
                        Color oldBg = GUI.backgroundColor;
                        GUI.backgroundColor = isDeleteHovered ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.85f, 0.25f, 0.25f, 1f);
                        GUI.Box(deleteBtnRect, TranslateText("Delete"), ThemeManager.SettingOff);
                        GUI.backgroundColor = oldBg;
                    }

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
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private class ProfileGUIService : IInitializable, IWarmUp, IMenuRenderable, ICloseHandler
    {
        public string Name => "ProfileGUI";
        public int Priority => ServicePriority.UI;

        public void Initialize() { }
        public void OnWarmUp() => ProfileGUI.Render();

        public void OnMenuGUI()
        {
            AnimationHandler.RenderWithTabAlpha(TabType.PROFILE, ProfileGUI.Render);
        }

        public bool CanClose()
        {
            return DrawSetting.ActiveTextFieldId == -1
                   && DrawSetting.FocusedControlId == -1;
        }

        public bool OnEscapePressed()
        {
            if (Config.CurrentTab != TabType.PROFILE) return false;

            if (DrawSetting.ActiveTextFieldId != -1 || DrawSetting.FocusedControlId != -1)
            {
                DrawSetting.ResetInputBind();
                Input.ResetInputAxes();
                return true;
            }

            return false;
        }
    }
}