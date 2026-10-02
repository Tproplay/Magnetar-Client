using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class SectionInstance
{
    public string Title;
    public bool IsExpanded = true;
    public List<Setting> ChildSettings = new();

    public SectionInstance(string title, List<Setting> settings)
    {
        Title = title;
        ChildSettings = settings ?? new List<Setting>();
    }

    /// <summary>
    /// Finds the first setting in ChildSettings by its name
    /// </summary>
    public Setting Find(string name)
    {
        if (ChildSettings == null || string.IsNullOrEmpty(name)) return null;

        foreach (var setting in ChildSettings)
        {
            if (setting != null && string.Equals(setting.Name, name, StringComparison.Ordinal))
            {
                return setting;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the first setting in ChildSettings by its name
    /// </summary>
    public bool Find(string name, out Setting setting)
    {
        setting = Find(name);
        return setting != null;
    }

    /// <summary>
    /// Finds the first setting in ChildSettings by its name
    /// </summary>
    public T Find<T>(string name) where T : Setting
    {
        return Find(name) as T;
    }

    /// <summary>
    /// Finds the first setting in ChildSettings by its name
    /// </summary>
    public bool Find<T>(string name, out T setting) where T : Setting
    {
        setting = Find(name) as T;
        return setting != null;
    }
}

public class SectionSetting : Setting
{
    public Func<int, List<Setting>> TemplateFactory { get; set; }
    public List<SectionInstance> Sections = new();
    public int MaxSections { get; set; } = -1;
    public int DefaultSectionCount { get; private set; } = 1;

    public Action OnSectionsChanged { get; set; }

    public SectionSetting(string name, Func<int, List<Setting>> templateFactory, int defaultSections = 1, int maxSections = -1)
    {
        Name = name;
        TemplateFactory = templateFactory;
        DefaultSectionCount = defaultSections;
        MaxSections = maxSections;
        Reset();
    }

    public override void Reset()
    {
        Sections.Clear();
        if (TemplateFactory != null)
        {
            for (int i = 0; i < DefaultSectionCount; i++)
            {
                Sections.Add(new SectionInstance($"Section #{i + 1}", TemplateFactory(i)));
            }
        }
        OnSectionsChanged?.Invoke();
    }

    public void AddSection()
    {
        if (MaxSections != -1 && Sections.Count >= MaxSections) return;
        int nextIdx = Sections.Count;
        List<Setting> newSettings = TemplateFactory != null ? TemplateFactory(nextIdx) : new List<Setting>();
        Sections.Add(new SectionInstance($"Section #{nextIdx + 1}", newSettings));
        OnSectionsChanged?.Invoke();
    }

    public void RemoveSection(int index)
    {
        if (index >= 0 && index < Sections.Count)
        {
            Sections.RemoveAt(index);
            OnSectionsChanged?.Invoke();
        }
    }

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float actionBtnW = Config.SettingsInput.ResetButtonW;

        // 1. Group Header
        string title = $"{Translator.Translate(Name)} ({Sections.Count})";
        Rect titleRect = new(indent, y, width - (indent * 2f), elemH);
        GUI.Box(titleRect, title, ThemeManager.SectionGroupHeaderStyle);
        y += elemH + gap;

        int removeIdx = -1;

        // 2. Render each section instance
        for (int i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            float sectionTotalW = width - (indent * 2f);
            float headerH = Config.S(24f);

            // Right-aligned action button
            float delStartX = indent + sectionTotalW - actionBtnW;
            float secHeaderW = sectionTotalW - actionBtnW - gap;

            Rect secHeaderRect = new(indent, y, secHeaderW, headerH);
            Rect delRect = new(delStartX, y, actionBtnW, headerH);

            string foldArrow = section.IsExpanded ? "▼ " : "▶ ";
            string secLabel = foldArrow + Translator.Translate(section.Title);

            // Sub-header bar
            if (GUI.Button(secHeaderRect, secLabel, ThemeManager.SectionHeaderStyle))
            {
                section.IsExpanded = !section.IsExpanded;
            }

            // Remove button
            GUI.Box(delRect, "—", ThemeManager.SectionRemoveButtonStyle);
            if (delRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                removeIdx = i;
                e.Use();
            }

            y += headerH + gap;

            // Render children if expanded
            if (section.IsExpanded)
            {
                for (int c = 0; c < section.ChildSettings.Count; c++)
                {
                    section.ChildSettings[c].Draw(ref y, width);
                }
            }

            y += gap;
        }

        if (removeIdx != -1)
        {
            RemoveSection(removeIdx);
        }

        // 3. Add Section & Reset Button Row
        bool canAdd = (MaxSections == -1 || Sections.Count < MaxSections);
        float resetStartX = width - indent - actionBtnW;
        float addBtnW = width - (indent * 2f) - actionBtnW - gap;

        Rect addBtnRect = new(indent, y, addBtnW, elemH);
        Rect resetRect = new(resetStartX, y, actionBtnW, elemH);

        GUIStyle addStyle = canAdd ? ThemeManager.SectionAddButtonStyle : ThemeManager.CategoryModuleOffStyle;
        string buttonText = canAdd ? Translator.Translate("+ Add Section") : Translator.Translate("Max Sections Reached");

        GUI.Box(addBtnRect, buttonText, addStyle);
        if (canAdd && e.type == EventType.MouseDown && e.button == 0 && addBtnRect.Contains(e.mousePosition))
        {
            e.Use();
            AddSection();
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}