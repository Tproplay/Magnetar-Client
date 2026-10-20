using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Core;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Represents an individual collapsible group within a <see cref="SectionSetting"/>, containing a title,
/// foldout state, and a collection of child <see cref="Setting"/> objects.
/// </summary>
public class SectionInstance
{
    /// <summary>
    /// The display title shown on the section's foldout header bar.
    /// </summary>
    public string Title;

    /// <summary>
    /// Whether this section's children are currently visible in the UI.
    /// </summary>
    public bool IsExpanded = true;

    /// <summary>
    /// The collection of child settings belonging to this specific section instance.
    /// </summary>
    public List<Setting> ChildSettings = new();

    /// <summary>
    /// Optional user data or metadata attached to this section instance.
    /// </summary>
    public object Tag { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SectionInstance"/> with a title and an optional list of child settings.
    /// </summary>
    /// <param name="title">The display title for the section header.</param>
    /// <param name="settings">The initial list of child settings for this section.</param>
    public SectionInstance(string title, List<Setting> settings = null)
    {
        Title = title;
        ChildSettings = settings ?? new List<Setting>();
    }

    /// <summary>
    /// Finds the first child setting matching the specified name.
    /// </summary>
    /// <param name="name">The name of the setting to locate.</param>
    /// <returns>The matching <see cref="Setting"/>, or <see langword="null"/> if not found.</returns>
    public Setting Find(string name)
    {
        if (ChildSettings == null || string.IsNullOrEmpty(name)) return null;

        for (int i = 0; i < ChildSettings.Count; i++)
        {
            var setting = ChildSettings[i];
            if (setting != null && string.Equals(setting.Name, name, StringComparison.Ordinal))
            {
                return setting;
            }
        }
        return null;
    }

    /// <summary>
    /// Attempts to find the first child setting matching the specified name.
    /// </summary>
    /// <param name="name">The name of the setting to locate.</param>
    /// <param name="setting">When this method returns, contains the matching setting if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a matching setting was found; otherwise, <see langword="false"/>.</returns>
    public bool Find(string name, out Setting setting)
    {
        setting = Find(name);
        return setting != null;
    }

    /// <summary>
    /// Finds the first child setting of type <typeparamref name="T"/> matching the specified name.
    /// </summary>
    /// <typeparam name="T">The expected <see cref="Setting"/> derived type.</typeparam>
    /// <param name="name">The name of the setting to locate.</param>
    /// <returns>The matching typed setting, or <see langword="null"/> if not found or the type does not match.</returns>
    public T Find<T>(string name) where T : Setting
    {
        return Find(name) as T;
    }

    /// <summary>
    /// Attempts to find the first child setting of type <typeparamref name="T"/> matching the specified name.
    /// </summary>
    /// <typeparam name="T">The expected <see cref="Setting"/> derived type.</typeparam>
    /// <param name="name">The name of the setting to locate.</param>
    /// <param name="setting">When this method returns, contains the matching typed setting if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a matching setting was found and successfully cast; otherwise, <see langword="false"/>.</returns>
    public bool Find<T>(string name, out T setting) where T : Setting
    {
        setting = Find(name) as T;
        return setting != null;
    }

    /// <summary>
    /// Adds a child setting to this section instance.
    /// </summary>
    /// <param name="setting">The setting to add.</param>
    public void Add(Setting setting)
    {
        if (setting != null) ChildSettings.Add(setting);
    }
}

/// <summary>
/// A dynamic, container setting that manages a repeatable list of collapsible <see cref="SectionInstance"/> groups,
/// supporting automated templating, count constraints, custom header formatting, and individual child drawing.
/// </summary>
public class SectionSetting : Setting, IEnumerable<SectionInstance>
{
    /// <summary>
    /// Factory delegate invoked to instantiate a fresh list of child settings for a newly added section.
    /// The integer parameter provides the 0-based index of the new section.
    /// </summary>
    public Func<int, List<Setting>> TemplateFactory { get; set; }

    /// <summary>
    /// Optional delegate to format or generate the display title for a section based on its 0-based index.
    /// If null, titles default to <c>"Section #{index + 1}"</c>.
    /// </summary>
    public Func<int, string> TitleFormatter { get; set; }

    /// <summary>
    /// The collection of currently instantiated section groups.
    /// </summary>
    public List<SectionInstance> Sections = new();

    /// <summary>
    /// The maximum number of allowed sections. Set to -1 for unlimited additions.
    /// </summary>
    public int MaxSections { get; set; } = -1;

    /// <summary>
    /// The minimum number of allowed sections. Prevents removal below this threshold.
    /// </summary>
    public int MinSections { get; set; } = 0;

    /// <summary>
    /// The number of sections populated when <see cref="Reset"/> is executed.
    /// </summary>
    public int DefaultSectionCount { get; set; } = 1;

    /// <summary>
    /// Whether users are permitted to manually add new sections via the UI.
    /// </summary>
    public bool AllowAdd { get; set; } = true;

    /// <summary>
    /// Whether users are permitted to manually remove sections via the UI.
    /// </summary>
    public bool AllowRemove { get; set; } = true;

    /// <summary>
    /// Whether newly created sections start in an expanded state.
    /// </summary>
    public bool DefaultExpanded { get; set; } = true;

    /// <summary>
    /// Whether to display the total section count in the group header label.
    /// </summary>
    public bool ShowCountInHeader { get; set; } = true;

    /// <summary>
    /// Whether to render the section group reset button in the footer controls row.
    /// </summary>
    public bool ShowResetButton { get; set; } = true;

    /// <summary>
    /// Custom text for the "+ Add Section" button. Defaults to <c>"+ Add Section"</c>.
    /// </summary>
    public string AddButtonText { get; set; } = "+ Add Section";

    /// <summary>
    /// Invoked whenever the structure of <see cref="Sections"/> changes (added, removed, or reset).
    /// </summary>
    public Action OnSectionsChanged { get; set; }

    /// <summary>
    /// Invoked immediately after a new section is added, passing the created <see cref="SectionInstance"/>.
    /// </summary>
    public Action<SectionInstance> OnSectionAdded { get; set; }

    /// <summary>
    /// Invoked immediately after a section is removed, passing its former index and instance.
    /// </summary>
    public Action<int, SectionInstance> OnSectionRemoved { get; set; }

    /// <summary>
    /// Gets the number of sections currently active.
    /// </summary>
    public int Count => Sections.Count;

    /// <summary>
    /// Gets the <see cref="SectionInstance"/> at the specified 0-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the section.</param>
    public SectionInstance this[int index] => Sections[index];

    /// <summary>
    /// Initializes a new instance of <see cref="SectionSetting"/>.
    /// </summary>
    /// <param name="name">The group display name.</param>
    /// <param name="templateFactory">Factory function generating child settings for each section.</param>
    /// <param name="defaultSections">The initial number of sections generated upon construction.</param>
    /// <param name="maxSections">The upper limit on total sections (-1 for unlimited).</param>
    public SectionSetting(string name, Func<int, List<Setting>> templateFactory, int defaultSections = 1, int maxSections = -1)
    {
        Name = name;
        TemplateFactory = templateFactory;
        DefaultSectionCount = defaultSections;
        MaxSections = maxSections;
        Reset();
    }

    /// <summary>
    /// Re-evaluates and applies <see cref="TitleFormatter"/> to all current sections if defined.
    /// </summary>
    public void RefreshTitles()
    {
        if (TitleFormatter == null) return;

        for (int i = 0; i < Sections.Count; i++)
        {
            Sections[i].Title = TitleFormatter(i);
        }
    }

    /// <summary>
    /// Clears and repopulates the section list back to <see cref="DefaultSectionCount"/> instances using <see cref="TemplateFactory"/>.
    /// </summary>
    public override void Reset()
    {
        Sections.Clear();
        if (TemplateFactory != null)
        {
            for (int i = 0; i < DefaultSectionCount; i++)
            {
                string title = TitleFormatter != null ? TitleFormatter(i) : $"Section #{i + 1}";
                var instance = new SectionInstance(title, TemplateFactory(i))
                {
                    IsExpanded = DefaultExpanded
                };
                Sections.Add(instance);
            }
        }
        OnSectionsChanged?.Invoke();
    }

    /// <summary>
    /// Instantiates and appends a new section using the configured <see cref="TemplateFactory"/>,
    /// provided the total section count has not reached <see cref="MaxSections"/>.
    /// </summary>
    /// <returns>The created <see cref="SectionInstance"/>, or <see langword="null"/> if blocked by limits.</returns>
    public SectionInstance AddSection()
    {
        if (MaxSections != -1 && Sections.Count >= MaxSections) return null;

        int nextIdx = Sections.Count;
        string title = TitleFormatter != null ? TitleFormatter(nextIdx) : $"Section #{nextIdx + 1}";
        List<Setting> newSettings = TemplateFactory != null ? TemplateFactory(nextIdx) : new List<Setting>();

        var instance = new SectionInstance(title, newSettings)
        {
            IsExpanded = DefaultExpanded
        };

        Sections.Add(instance);
        OnSectionAdded?.Invoke(instance);
        OnSectionsChanged?.Invoke();
        return instance;
    }

    /// <summary>
    /// Appends an explicitly created <see cref="SectionInstance"/> to the section collection.
    /// </summary>
    /// <param name="section">The section instance to add.</param>
    /// <returns><see langword="true"/> if added; <see langword="false"/> if rejected by <see cref="MaxSections"/>.</returns>
    public bool AddSection(SectionInstance section)
    {
        if (section == null) return false;
        if (MaxSections != -1 && Sections.Count >= MaxSections) return false;

        Sections.Add(section);
        RefreshTitles();
        OnSectionAdded?.Invoke(section);
        OnSectionsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes the section at the specified index, respecting <see cref="MinSections"/> and <see cref="AllowRemove"/>.
    /// </summary>
    /// <param name="index">The 0-based index of the section to remove.</param>
    /// <returns><see langword="true"/> if successfully removed; otherwise, <see langword="false"/>.</returns>
    public bool RemoveSection(int index)
    {
        if (!AllowRemove) return false;
        if (MinSections != -1 && Sections.Count <= MinSections) return false;

        if (index >= 0 && index < Sections.Count)
        {
            var removed = Sections[index];
            Sections.RemoveAt(index);
            RefreshTitles();
            OnSectionRemoved?.Invoke(index, removed);
            OnSectionsChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all sections down to <see cref="MinSections"/>.
    /// </summary>
    public void ClearSections()
    {
        while (Sections.Count > Math.Max(0, MinSections))
        {
            Sections.RemoveAt(Sections.Count - 1);
        }
        RefreshTitles();
        OnSectionsChanged?.Invoke();
    }

    /// <summary>
    /// Adjusts the active section count to match <paramref name="targetCount"/> by adding or removing sections.
    /// </summary>
    /// <param name="targetCount">The desired number of active sections.</param>
    public void SetSectionCount(int targetCount)
    {
        if (MaxSections != -1) targetCount = Math.Min(targetCount, MaxSections);
        if (MinSections != -1) targetCount = Math.Max(targetCount, MinSections);

        while (Sections.Count < targetCount)
        {
            AddSection();
        }

        while (Sections.Count > targetCount)
        {
            RemoveSection(Sections.Count - 1);
        }
    }

    /// <summary>
    /// Sets the expanded foldout state across all existing sections.
    /// </summary>
    /// <param name="isExpanded"><see langword="true"/> to expand all; <see langword="false"/> to collapse all.</param>
    public void SetAllExpanded(bool isExpanded)
    {
        for (int i = 0; i < Sections.Count; i++)
        {
            Sections[i].IsExpanded = isExpanded;
        }
    }

    /// <summary>
    /// Renders the section group header, each child section's sub-header bar, child settings,
    /// and the footer row for section creation and resetting.
    /// </summary>
    /// <param name="y">Reference to the current vertical layout offset.</param>
    /// <param name="width">The total width allocated for rendering.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float actionBtnW = SettingValues.ResetButtonW;

        // 1. Group Header[cite: 16]
        string countSuffix = ShowCountInHeader ? $" ({Sections.Count})" : "";
        string title = $"{Translate(Name)}{countSuffix}";
        Rect titleRect = new(indent, y, width - (indent * 2f), elemH);
        GUI.Box(titleRect, title, ThemeManager.SectionGroupHeaderStyle);
        y += elemH + gap;

        int removeIdx = -1;
        bool canRemoveAny = AllowRemove && (MinSections == -1 || Sections.Count > MinSections);

        // 2. Render each section instance[cite: 16]
        for (int i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            float sectionTotalW = width - (indent * 2f);
            float headerH = GUIManager.S(24f);

            Rect secHeaderRect;
            Rect delRect = default;

            if (canRemoveAny)
            {
                float delStartX = indent + sectionTotalW - actionBtnW;
                float secHeaderW = sectionTotalW - actionBtnW - gap;

                secHeaderRect = new Rect(indent, y, secHeaderW, headerH);
                delRect = new Rect(delStartX, y, actionBtnW, headerH);
            }
            else
            {
                secHeaderRect = new Rect(indent, y, sectionTotalW, headerH);
            }

            string foldArrow = section.IsExpanded ? "▼ " : "▶ ";
            string secLabel = foldArrow + Translate(section.Title);

            // Sub-header bar box visual
            GUI.Box(secHeaderRect, secLabel, Magnetar_Default.SectionHeaderStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && secHeaderRect.Contains(e.mousePosition))
            {
                e.Use();
                section.IsExpanded = !section.IsExpanded;
            }

            // Remove button[cite: 16]
            if (canRemoveAny)
            {
                GUI.Box(delRect, "—", ThemeManager.SectionRemoveButtonStyle);
                if (delRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
                {
                    removeIdx = i;
                    e.Use();
                }
            }

            y += headerH + gap;

            // Render children if expanded[cite: 16]
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

        // 3. Add Section & Reset Button Row[cite: 16]
        if (AllowAdd || ShowResetButton)
        {
            bool canAdd = AllowAdd && (MaxSections == -1 || Sections.Count < MaxSections);
            float resetStartX = width - indent - actionBtnW;

            Rect addBtnRect;
            Rect resetRect = new(resetStartX, y, actionBtnW, elemH);

            if (ShowResetButton && AllowAdd)
            {
                float addBtnW = width - (indent * 2f) - actionBtnW - gap;
                addBtnRect = new Rect(indent, y, addBtnW, elemH);
            }
            else if (AllowAdd)
            {
                addBtnRect = new Rect(indent, y, width - (indent * 2f), elemH);
            }
            else
            {
                addBtnRect = Rect.zero;
            }

            if (AllowAdd)
            {
                GUIStyle addStyle = canAdd ? ThemeManager.SectionAddButtonStyle : ThemeManager.CategoryModuleOffStyle;
                string buttonText = canAdd ? Translate(AddButtonText) : Translate("Max Sections Reached");

        GUIStyle addStyle = canAdd ? Magnetar_Default.SectionAddButtonStyle : Magnetar_Default.CategoryModuleOffStyle;
        string buttonText = canAdd ? Translator.Translate("+ Add Section") : Translator.Translate("Max Sections Reached");

        GUI.Box(addBtnRect, buttonText, addStyle);
        if (canAdd && e.type == EventType.MouseDown && e.button == 0 && addBtnRect.Contains(e.mousePosition))
        {
            e.Use();
            AddSection();
        }

            if (ShowResetButton && DrawResetButton(resetRect))
            {
                Reset();
            }

            y += elemH + ClientConfig.spacing;
        }
        else
        {
            y += ClientConfig.spacing;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the active <see cref="SectionInstance"/> list.
    /// </summary>
    public IEnumerator<SectionInstance> GetEnumerator() => Sections.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}