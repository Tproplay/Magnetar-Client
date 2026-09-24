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
        float actionBtnW = Config.S(24f);
        float gap = Config.S(4f);

        // 1. Group Header
        string title = $"{Translator.Translate(Name)} ({Sections.Count})";
        Rect titleRect = new(Config.indent, y, width - (Config.indent * 2), elemH);
        GUI.Box(titleRect, title, Magnetar_Default.CategoryHeaderStyle);
        y += elemH + gap;

        int removeIdx = -1;

        // 2. Render each section instance
        for (int i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            float sectionWidth = width - (Config.indent * 2);

            // Sub-header bar
            Rect secHeaderRect = new(Config.indent, y, sectionWidth - actionBtnW - gap, Config.S(24f));
            Rect delRect = new(Config.indent + sectionWidth - actionBtnW, y, actionBtnW, Config.S(24f));

            string foldArrow = section.IsExpanded ? "▼ " : "▶ ";
            string secLabel = foldArrow + Translator.Translate(section.Title);

            if (GUI.Button(secHeaderRect, secLabel, Magnetar_Default.CategoryModuleOffStyle))
            {
                section.IsExpanded = !section.IsExpanded;
            }

            GUI.Box(delRect, "—", Magnetar_Default.SettingOff);
            if (delRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                removeIdx = i;
                e.Use();
            }

            y += Config.S(24f) + gap;

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
        float addBtnW = width - (Config.indent * 2) - actionBtnW - gap;

        Rect addBtnRect = new(Config.indent, y, addBtnW, elemH);
        Rect resetRect = new(Config.indent + addBtnW + gap, y, actionBtnW, elemH);

        GUIStyle addStyle = canAdd ? Magnetar_Default.SettingOn : Magnetar_Default.CategoryModuleOffStyle;
        if (GUI.Button(addBtnRect, canAdd ? Translator.Translate("+ Add Section") : Translator.Translate("Max Sections Reached"), addStyle))
        {
            if (canAdd) AddSection();
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}