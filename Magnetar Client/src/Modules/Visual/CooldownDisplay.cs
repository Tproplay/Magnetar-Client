using System;
using System.Collections.Generic;
using System.Globalization;
using Magnetar_Client.UI;
using Magnetar_Client.UI.Setting;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
using Il2CppTMPro;
#elif BEPINEX || RELEASE_BEPINEX
using TMPro;
#endif

namespace Magnetar_Client.Modules;

public class CooldownDisplay : Module
{
    // Mod Info
    public override string Name { get; set; } = "Cooldown Display";
    public override string Description { get; set; } = "Displays the remaining cooldown on tools and cards.";
    public override string SearchHints { get; set; } = "cddisplay cooldown displaycooldown cooldowntimer cooldownoverlay timerdisplay " +
        "cooldownhud cooldowntracker cardcooldown toolcooldown cooldowndisplayer abilitycooldown cooldowncounter cooldownmonitor " +
        "cooldownshow cooldownstatus cooldownview skillcooldown cooldownindicator";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Visual;
    public override bool EnableInVanillaMode { get; set; } = true;

    public static CooldownDisplay instance;

    /// <summary>
    /// Autocomplete variable names available inside <see cref="FormatString"/>.
    /// </summary>
    public static readonly List<string> AutoCompleteArgs = new()
    {
        "Remaining",
        "RemainingCeil",
        "Elapsed",
        "Total",
        "Percent",
        "Progress",
        "Speed"
    };

    // General Settings
    public BoolSetting ShowCards;
    public BoolSetting ShowHammer;
    public BoolSetting ShowGlove;
    public BoolSetting ShowWheel;
    public BoolSetting ShowOnIZCards;

    // Format Settings
    public StringSetting FormatString;
    public BoolSetting SpeedAdjusted;
    public IntSetting DecimalPlaces;

    // Appearance Settings
    public StyleSetting CardStyle;
    public StyleSetting ToolStyle;

    private sealed class CooldownOverlay
    {
        public GameObject LabelObject;
        public TextMeshProUGUI Text;
        public bool IsTool;
        public int LastSeenFrame;
        public int AppliedStyleVersion = -1;
        public int LastRemKey = int.MinValue;
        public int LastCeilKey = int.MinValue;
        public int LastElapsedKey = int.MinValue;
        public int LastTotalKey = int.MinValue;
        public int LastPctKey = int.MinValue;
        public int LastProgKey = int.MinValue;
        public int LastSpeedKey = int.MinValue;
        public string LastText;
    }

    private readonly Dictionary<IntPtr, CooldownOverlay> _overlays = new();
    private readonly List<IntPtr> _staleKeys = new();

    private TMP_FontAsset _cachedFont;
    private int _styleVersion;

    // Cached tool references for active UI banks (survives SandboxUI/InGameUI switching)
    private InGameUI _cachedNormalUI;
    private Hammer _cachedNormalHammer;
    private Glove _cachedNormalGlove;
    private Wheel _cachedNormalWheel;

    private InGameUI_IZ _cachedIZUI;
    private Glove _cachedIZGlove;
    private Wheel _cachedIZWheel;

    private const string OverlayObjectName = "MagnetarCooldownLabel";

    public CooldownDisplay()
    {
        instance = this;

        CreateCategory("General");
        ShowCards = new BoolSetting("Show on Cards", true);
        ShowHammer = new BoolSetting("Show on Hammer", true);
        ShowGlove = new BoolSetting("Show on Glove", true);
        ShowWheel = new BoolSetting("Show on Wheel Barrow", true);
        ShowOnIZCards = new BoolSetting("Show on I-Zombie Cards", true);
        AddSettings(ShowCards, ShowHammer, ShowGlove, ShowWheel, ShowOnIZCards);
        EndCategory();

        CreateCategory("Format");
        FormatString = new StringSetting("Display Format", "{Remaining}s", AutoCompleteArgs)
        {
            OnValueChanged = _ => InvalidateAllDisplayCache()
        };
        SpeedAdjusted = new BoolSetting("Speed-Adjusted Time", true)
        {
            OnValueChanged = _ => InvalidateAllDisplayCache()
        };
        DecimalPlaces = new IntSetting("Decimal Places", 0, 3, 1, 0, 3, true, false)
        {
            OnValueChanged = _ => InvalidateAllDisplayCache()
        };
        AddSettings(FormatString, SpeedAdjusted, DecimalPlaces);
        EndCategory();

        CreateCategory("Appearance");
        CardStyle = new StyleSetting(
            "Card Style",
            StyleFeatures.TextColor | StyleFeatures.FontSize | StyleFeatures.Outline,
            new StyleDefaults
            {
                TextColor = new Color(255f / 255f, 255f / 255f, 38f / 255f, 1f),
                FontSize = 20f,
                MinFontSize = 8f,
                MaxFontSize = 60f,
                FontSizePrecision = 1,
                OutlineEnabled = true,
                OutlineColor = new Color(0f, 0f, 0f, 1f),
                OutlineWidth = 0.25f,
                MinOutlineWidth = 0f,
                MaxOutlineWidth = 0.5f,
                OutlineWidthPrecision = 2
            })
        {
            OnStyleChanged = _ => MarkStyleDirty()
        };

        ToolStyle = new StyleSetting(
            "Tool Style",
            StyleFeatures.TextColor | StyleFeatures.FontSize | StyleFeatures.Outline,
            new StyleDefaults
            {
                TextColor = new Color(255f / 255f, 255f / 255f, 255f / 255f, 1f),
                FontSize = 12f,
                MinFontSize = 8f,
                MaxFontSize = 60f,
                FontSizePrecision = 1,
                OutlineEnabled = true,
                OutlineColor = new Color(0f, 0f, 0f, 1f),
                OutlineWidth = 0.25f,
                MinOutlineWidth = 0f,
                MaxOutlineWidth = 0.5f,
                OutlineWidthPrecision = 2
            })
        {
            OnStyleChanged = _ => MarkStyleDirty()
        };

        AddSettings(CardStyle, ToolStyle);
        EndCategory();

        GameData.Actions.OnBoardDestroyed += ClearAllOverlays;
        GameData.Actions.OnBoardDied += ClearAllOverlays;
        GameData.Actions.OnInGameUIDestroyed += ClearCachedReferences;
    }

    public override void OnDisable()
    {
        HideAllOverlays();
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull || Board.Instance == null)
        {
            if (_overlays.Count > 0) ClearAllOverlays();
            return;
        }

        if (GameAPP.theGameStatus == GameStatus.Selecting)
        {
            HideAllOverlays();
            return;
        }

        int frame = Time.frameCount;

        if (ShowCards.Value)
        {
            UpdateNormalCards(frame);
            if (ShowOnIZCards.Value)
            {
                UpdateIZCards(frame);
            }
        }

        UpdateTools(frame);
        SweepUnusedOverlays(frame);
    }

    private void UpdateNormalCards(int frame)
    {
        var ui = InGameUI.Instance;
        if (ui == null || ui.gameObject == null || !ui.gameObject.activeInHierarchy || ui.Cards == null)
            return;

        for (int i = 0; i < ui.Cards.Count; i++)
        {
            ProcessCard(ui.Cards[i], frame);
        }
    }

    private void UpdateIZCards(int frame)
    {
        var izUI = InGameUI_IZ.Instance;
        if (izUI == null || izUI.gameObject == null || !izUI.gameObject.activeInHierarchy)
            return;

        var cards = izUI.Cards;
        if (cards == null) return;

        for (int i = 0; i < cards.Count; i++)
        {
            ProcessCard(cards[i], frame);
        }
    }

    private void ProcessCard(CardUI card, int frame)
    {
        if (card == null || card.gameObject == null || !card.gameObject.activeInHierarchy)
            return;
        if (!card.onCardBank || card.fullCD <= 0f || card.CD >= card.fullCD)
            return;
        if (card.TryCast<DroppedCard>() != null)
            return;

        if (_cachedFont == null && card.textBg != null)
        {
            _cachedFont = card.textBg.font;
        }

        float speed = SpeedAdjusted.Value ? GetCardCooldownSpeed(card) : 1f;
        UpdateOverlay(
            card.Pointer,
            card.transform,
            isTool: false,
            card.CD,
            card.fullCD,
            speed,
            frame,
            card.textBg
        );
    }

    private void UpdateTools(int frame)
    {
        var normalUI = InGameUI.Instance;
        if (normalUI != _cachedNormalUI)
        {
            _cachedNormalUI = normalUI;
            _cachedNormalHammer = null;
            _cachedNormalGlove = null;
            _cachedNormalWheel = null;
        }

        var izUI = InGameUI_IZ.Instance;
        if (izUI != _cachedIZUI)
        {
            _cachedIZUI = izUI;
            _cachedIZGlove = null;
            _cachedIZWheel = null;
        }

        bool normalActive = normalUI != null && normalUI.gameObject != null && normalUI.gameObject.activeInHierarchy;
        bool izActive = izUI != null && izUI.gameObject != null && izUI.gameObject.activeInHierarchy;

        if (ShowHammer.Value)
        {
            Hammer hammer = Hammer.Instance;
            if ((hammer == null || hammer.gameObject == null || !hammer.gameObject.activeInHierarchy) && normalActive)
            {
                hammer = ResolveBankTool(normalUI.HammerBank, ref _cachedNormalHammer);
            }
            ProcessTool(hammer, frame);
        }

        if (ShowGlove.Value)
        {
            Glove glove = Glove.Instance;
            if ((glove == null || glove.gameObject == null || !glove.gameObject.activeInHierarchy) && normalActive)
            {
                glove = ResolveBankTool(normalUI.GloveBank, ref _cachedNormalGlove);
            }
            if ((glove == null || glove.gameObject == null || !glove.gameObject.activeInHierarchy) && izActive)
            {
                glove = ResolveBankTool(izUI.glove, ref _cachedIZGlove);
            }
            ProcessTool(glove, frame);
        }

        if (ShowWheel.Value)
        {
            Wheel wheel = GameData.WheelInstance;
            if ((wheel == null || wheel.gameObject == null || !wheel.gameObject.activeInHierarchy) && normalActive)
            {
                wheel = ResolveBankTool(normalUI.WheelBank, ref _cachedNormalWheel);
            }
            if ((wheel == null || wheel.gameObject == null || !wheel.gameObject.activeInHierarchy) && izActive)
            {
                wheel = ResolveBankTool(izUI.wheel, ref _cachedIZWheel);
            }
            ProcessTool(wheel, frame);
        }
    }

    private static T ResolveBankTool<T>(GameObject bank, ref T cached) where T : InGameTool
    {
        if (cached != null && cached.gameObject != null) return cached;
        if (bank == null) return null;
        cached = bank.GetComponentInChildren<T>(true);
        return cached;
    }

    private void ProcessTool(InGameTool tool, int frame)
    {
        if (tool == null || tool.gameObject == null || tool.isPickUp)
            return;

        Transform anchor = tool.defaultParent != null ? tool.defaultParent.transform : tool.transform;
        if (anchor == null || !anchor.gameObject.activeInHierarchy)
            return;

        if (tool.fullCD <= 0f || tool.CD >= tool.fullCD)
            return;

        float speed = SpeedAdjusted.Value ? GetToolCooldownSpeed(tool) : 1f;
        UpdateOverlay(
            tool.Pointer,
            anchor,
            isTool: true,
            tool.CD,
            tool.fullCD,
            speed,
            frame,
            fallbackTextSource: null
        );
    }

    private void UpdateOverlay(
        IntPtr key,
        Transform parent,
        bool isTool,
        float elapsed,
        float fullCD,
        float speed,
        int frame,
        TextMeshProUGUI fallbackTextSource)
    {
        if (parent == null) return;

        if (!_overlays.TryGetValue(key, out CooldownOverlay overlay) ||
            overlay.LabelObject == null ||
            overlay.Text == null ||
            overlay.LabelObject.transform.parent != parent)
        {
            if (overlay != null && overlay.LabelObject != null)
            {
                UnityEngine.Object.Destroy(overlay.LabelObject);
            }

            overlay = CreateOverlay(parent, isTool, fallbackTextSource);
            if (overlay == null) return;
            _overlays[key] = overlay;
        }

        overlay.LastSeenFrame = frame;

        if (!overlay.LabelObject.activeSelf)
        {
            overlay.LabelObject.SetActive(true);
        }

        if (overlay.LabelObject.transform.GetSiblingIndex() != parent.childCount - 1)
        {
            overlay.LabelObject.transform.SetAsLastSibling();
        }

        if (overlay.AppliedStyleVersion != _styleVersion)
        {
            ApplyStyle(overlay);
        }

        float safeSpeed = speed > 0.0001f ? speed : 1f;
        float rawRemaining = Mathf.Max(0f, fullCD - elapsed);
        float remaining = SpeedAdjusted.Value ? (rawRemaining / safeSpeed) : rawRemaining;

        UpdateOverlayText(overlay, remaining, elapsed, fullCD, safeSpeed);
    }

    private CooldownOverlay CreateOverlay(Transform parent, bool isTool, TextMeshProUGUI fallbackTextSource)
    {
        TMP_FontAsset font = ResolveFontAsset(fallbackTextSource);

        GameObject labelObj = new(OverlayObjectName);
        RectTransform rect = labelObj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetAsLastSibling();

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }

        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.richText = true;

        var overlay = new CooldownOverlay
        {
            LabelObject = labelObj,
            Text = tmp,
            IsTool = isTool
        };
        ApplyStyle(overlay);
        return overlay;
    }

    private void ApplyStyle(CooldownOverlay overlay)
    {
        TextMeshProUGUI tmp = overlay.Text;
        if (tmp == null) return;

        StyleSetting style = overlay.IsTool ? ToolStyle : CardStyle;
        style?.ApplyTo(tmp);

        overlay.AppliedStyleVersion = _styleVersion;
    }

    public override void OnSettingsRestored()
    {
        if (Core.ModuleManager_.ModuleSaveHandler.CachedModuleData != null &&
            Core.ModuleManager_.ModuleSaveHandler.CachedModuleData.TryGetValue(Name, out var modData) &&
            modData?.Settings != null)
        {
            if (!modData.Settings.ContainsKey(CardStyle.SaveKey) && modData.Settings.ContainsKey("Card Font Size"))
            {
                if (modData.Settings.TryGetValue("Text Color", out var tc) && tc != null && CardStyle.TextColorSetting != null)
                {
                    CardStyle.TextColorSetting.HexValue = tc.ToString();
                    if (ToolStyle.TextColorSetting != null) ToolStyle.TextColorSetting.HexValue = tc.ToString();
                }
                if (modData.Settings.TryGetValue("Card Font Size", out var cfs) && cfs != null)
                {
                    CardStyle.FontSize = Convert.ToSingle(cfs);
                }
                if (modData.Settings.TryGetValue("Tool Font Size", out var tfs) && tfs != null)
                {
                    ToolStyle.FontSize = Convert.ToSingle(tfs);
                }
                if (modData.Settings.TryGetValue("Text Outline", out var to) && to != null)
                {
                    bool outline = Convert.ToBoolean(to);
                    CardStyle.OutlineEnabled = outline;
                    ToolStyle.OutlineEnabled = outline;
                }
                if (modData.Settings.TryGetValue("Outline Color", out var oc) && oc != null && CardStyle.OutlineColorSetting != null)
                {
                    CardStyle.OutlineColorSetting.HexValue = oc.ToString();
                    if (ToolStyle.OutlineColorSetting != null) ToolStyle.OutlineColorSetting.HexValue = oc.ToString();
                }
                if (modData.Settings.TryGetValue("Outline Width", out var ow) && ow != null)
                {
                    float width = Convert.ToSingle(ow);
                    CardStyle.OutlineWidth = width;
                    ToolStyle.OutlineWidth = width;
                }
                CardStyle.MarkDirty();
                ToolStyle.MarkDirty();
                MarkStyleDirty();
            }
        }
    }

    private void UpdateOverlayText(CooldownOverlay overlay, float remaining, float elapsed, float total, float speed)
    {
        int decimals = Math.Clamp(DecimalPlaces.Value, 0, 3);
        float scale = decimals switch
        {
            0 => 1f,
            1 => 10f,
            2 => 100f,
            _ => 1000f
        };

        int remKey = Mathf.RoundToInt(remaining * scale);
        int ceilKey = Mathf.CeilToInt(remaining);
        int elapsedKey = Mathf.RoundToInt(elapsed * scale);
        int totalKey = Mathf.RoundToInt(total * scale);
        int pctKey = total > 0f ? Mathf.Clamp(Mathf.RoundToInt((1f - (elapsed / total)) * 100f), 0, 100) : 0;
        int progKey = total > 0f ? Mathf.Clamp(Mathf.RoundToInt((elapsed / total) * 100f), 0, 100) : 100;
        int speedKey = Mathf.RoundToInt(speed * 100f);

        if (overlay.LastText != null &&
            overlay.LastRemKey == remKey &&
            overlay.LastCeilKey == ceilKey &&
            overlay.LastElapsedKey == elapsedKey &&
            overlay.LastTotalKey == totalKey &&
            overlay.LastPctKey == pctKey &&
            overlay.LastProgKey == progKey &&
            overlay.LastSpeedKey == speedKey)
        {
            return;
        }

        overlay.LastRemKey = remKey;
        overlay.LastCeilKey = ceilKey;
        overlay.LastElapsedKey = elapsedKey;
        overlay.LastTotalKey = totalKey;
        overlay.LastPctKey = pctKey;
        overlay.LastProgKey = progKey;
        overlay.LastSpeedKey = speedKey;

        string numFormat = decimals switch
        {
            0 => "0",
            1 => "0.0",
            2 => "0.00",
            _ => "0.000"
        };

        string template = string.IsNullOrEmpty(FormatString.Value) ? "{Remaining}s" : FormatString.Value;
        string formatted = template;

        if (formatted.IndexOf('{') >= 0)
        {
            formatted = ReplacePlaceholder(formatted, "{Remaining}", remaining.ToString(numFormat, CultureInfo.InvariantCulture));
            formatted = ReplacePlaceholder(formatted, "{RemainingCeil}", ceilKey.ToString(CultureInfo.InvariantCulture));
            formatted = ReplacePlaceholder(formatted, "{Elapsed}", elapsed.ToString(numFormat, CultureInfo.InvariantCulture));
            formatted = ReplacePlaceholder(formatted, "{Total}", total.ToString(numFormat, CultureInfo.InvariantCulture));
            formatted = ReplacePlaceholder(formatted, "{Percent}", pctKey.ToString(CultureInfo.InvariantCulture));
            formatted = ReplacePlaceholder(formatted, "{Progress}", progKey.ToString(CultureInfo.InvariantCulture));
            formatted = ReplacePlaceholder(formatted, "{Speed}", speed.ToString("0.##", CultureInfo.InvariantCulture));
        }

        if (formatted.IndexOf("\\n", StringComparison.Ordinal) >= 0)
        {
            formatted = formatted.Replace("\\n", "\n");
        }

        if (overlay.LastText != formatted)
        {
            overlay.LastText = formatted;
            overlay.Text.text = formatted;
        }
    }

    private static string ReplacePlaceholder(string source, string token, string value)
    {
        return source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0
            ? source.Replace(token, value, StringComparison.OrdinalIgnoreCase)
            : source;
    }

    private static float GetCardCooldownSpeed(CardUI card)
    {
        if (card.hoverGardenCard != null) return 1f;

        float speed = 1f;
        try
        {
            if (Lawnf.TravelAdvanced((AdvBuff)1001))
            {
                speed += 1f;
            }

            if (card.board != null && card.board.theWave == 0 &&
                AdvantureConfig.data != null &&
                AdvantureConfig.data.GetResult(AdvantureLevel.Roof2, MissionResult.First))
            {
                speed += 0.2f;
            }
        }
        catch { }

        return speed > 0.0001f ? speed : 1f;
    }

    private static float GetToolCooldownSpeed(InGameTool tool)
    {
        float speed = tool.coolSpeed;
        try
        {
            if (tool.TryCast<Hammer>() != null)
            {
                if (Lawnf.TravelAdvanced((AdvBuff)1005))
                {
                    speed += 9f;
                }
                if (Lawnf.TravelAdvanced((AdvBuff)13010))
                {
                    speed += 19f;
                    if (Lawnf.TravelAdvanced((AdvBuff)13012))
                    {
                        speed += 19f;
                    }
                }
            }
            else if (tool.TryCast<Glove>() != null)
            {
                if (Lawnf.TravelAdvanced((AdvBuff)1003))
                {
                    speed += 1f;
                }
            }
        }
        catch { }

        return speed > 0.0001f ? speed : 1f;
    }

    private TMP_FontAsset ResolveFontAsset(TextMeshProUGUI fallbackSource)
    {
        if (_cachedFont != null) return _cachedFont;

        try
        {
            // Primary font: Generate TMP font from ResourceManager.DefaultFont
            if (ResourceManager.DefaultFontAsset != null)
            {
                _cachedFont = ResourceManager.DefaultFontAsset;
                return _cachedFont;
            }

            Font defaultFont = ResourceManager.DefaultFont;
            if (defaultFont != null)
            {
                _cachedFont = TMP_FontAsset.CreateFontAsset(defaultFont);
                if (_cachedFont != null)
                {
                    _cachedFont.hideFlags = HideFlags.DontSave;
                    return _cachedFont;
                }
            }
        }
        catch { }

        // Secondary fallback: Existing UI components (cards / sun counter)
        if (fallbackSource != null && fallbackSource.font != null)
        {
            _cachedFont = fallbackSource.font;
            return _cachedFont;
        }

        if (InGameUI.Instance?.sun != null && InGameUI.Instance.sun.font != null)
        {
            _cachedFont = InGameUI.Instance.sun.font;
            return _cachedFont;
        }

        if (InGameUI_IZ.Instance?.sun != null && InGameUI_IZ.Instance.sun.font != null)
        {
            _cachedFont = InGameUI_IZ.Instance.sun.font;
            return _cachedFont;
        }

        if (_cachedFont == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                _cachedFont = allFonts[0];
            }
        }

        return _cachedFont;
    }

    private void SweepUnusedOverlays(int currentFrame)
    {
        _staleKeys.Clear();

        foreach (var kvp in _overlays)
        {
            CooldownOverlay overlay = kvp.Value;
            if (overlay.LabelObject == null || overlay.Text == null)
            {
                _staleKeys.Add(kvp.Key);
                continue;
            }

            if (overlay.LastSeenFrame != currentFrame && overlay.LabelObject.activeSelf)
            {
                overlay.LabelObject.SetActive(false);
            }
        }

        for (int i = 0; i < _staleKeys.Count; i++)
        {
            _overlays.Remove(_staleKeys[i]);
        }
    }

    private void HideAllOverlays()
    {
        _staleKeys.Clear();

        foreach (var kvp in _overlays)
        {
            if (kvp.Value.LabelObject == null)
            {
                _staleKeys.Add(kvp.Key);
                continue;
            }

            if (kvp.Value.LabelObject.activeSelf)
            {
                kvp.Value.LabelObject.SetActive(false);
            }
        }

        for (int i = 0; i < _staleKeys.Count; i++)
        {
            _overlays.Remove(_staleKeys[i]);
        }
    }

    private void ClearAllOverlays()
    {
        foreach (var kvp in _overlays)
        {
            if (kvp.Value.LabelObject != null)
            {
                UnityEngine.Object.Destroy(kvp.Value.LabelObject);
            }
        }

        _overlays.Clear();
        ClearCachedReferences();
    }

    private void ClearCachedReferences()
    {
        _cachedNormalUI = null;
        _cachedNormalHammer = null;
        _cachedNormalGlove = null;
        _cachedNormalWheel = null;
        _cachedIZUI = null;
        _cachedIZGlove = null;
        _cachedIZWheel = null;
    }

    private void MarkStyleDirty()
    {
        _styleVersion++;
    }

    private void InvalidateAllDisplayCache()
    {
        foreach (var kvp in _overlays)
        {
            kvp.Value.LastText = null;
        }
    }
}
