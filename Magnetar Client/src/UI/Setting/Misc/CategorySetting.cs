using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A collapsible group header setting that organizes subsequent settings into an expandable category.
/// </summary>
public class CategorySetting : Setting
{
    /// <summary>
    /// Whether the category is currently expanded and rendering its child settings.
    /// </summary>
    public bool IsExpanded;

    /// <summary>
    /// The default expanded state restored when <see cref="Reset"/> is called.
    /// </summary>
    public bool DefaultExpanded;

    /// <summary>
    /// Indicates that this category header supports resetting to its default expanded state.
    /// </summary>
    public override bool CanReset => true;

    /// <summary>
    /// Gets the unique persistence key used when saving and loading this category header setting.
    /// </summary>
    public override string SaveKey => !string.IsNullOrEmpty(CustomSaveKey)
        ? CustomSaveKey
        : (Name + "_Category");

    /// <summary>
    /// Initializes a new instance of <see cref="CategorySetting"/>.
    /// </summary>
    /// <param name="name">The title of the category.</param>
    /// <param name="defaultExpanded">Whether the category begins in an expanded state by default.</param>
    public CategorySetting(string name, bool defaultExpanded = true)
    {
        Name = name;
        DefaultExpanded = defaultExpanded;
        IsExpanded = defaultExpanded;
    }

    /// <summary>
    /// Restores the expanded state back to <see cref="DefaultExpanded"/>.
    /// </summary>
    public override void Reset()
    {
        IsExpanded = DefaultExpanded;
    }

    /// <summary>
    /// Renders the category separator bar and handles toggle interactions in the IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        IsExpanded = MiscDrawing.Seperator(
            ref y,
            width,
            ClientConfig.indent,
            ClientConfig.spacing,
            Translate(Name),
            true,
            IsExpanded
        );
    }
}

/// <summary>
/// A marker setting denoting the end of an open category block, terminating child indentation and skipping.
/// </summary>
public class EndCategorySetting : Setting
{
    /// <summary>
    /// Indicates that boundary markers cannot be reset.
    /// </summary>
    public override bool CanReset => false;

    /// <summary>
    /// Performs no rendering; functions purely as a structural delimiter in the settings sequence.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width) { }
}