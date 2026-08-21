using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Digi21.WinUI.Ribbon;

/// <summary>A heading over a set of contextual tabs: Office's coloured band, with a name and a colour of its own.</summary>
/// <remarks>
/// <para>
/// One of these is the answer to a question a single accent line cannot answer: that these tabs
/// belong together, and that they are here because of something that has just happened. Point any
/// number of contextual tabs at the same instance and they are drawn under one band; point one at it
/// and the band is over that one, which is the ordinary case and is what Office does with a lone
/// contextual tab too.
/// </para>
/// <para>
/// The band is drawn only while at least one of its tabs is on the strip, and it changes nothing
/// about the height of the ribbon: the room for it is held from the moment a tab declares a group,
/// whether or not that tab is switched on, because a strip that grew as a tab arrived would push the
/// window down at the moment somebody was reaching into it.
/// </para>
/// <para>
/// It is an ordinary object rather than a control. Declare it as a resource and share it between
/// tabs, or make one in code and hand it to the tab that needs it.
/// </para>
/// </remarks>
/// <example>
/// <code lang="xml">
/// &lt;Page.Resources&gt;
///     &lt;ribbon:RibbonContextualGroup x:Key="SelectionTools" Label="Selection tools" Accent="#C50F1F" /&gt;
/// &lt;/Page.Resources&gt;
///
/// &lt;ribbon:RibbonTab
///     Label="Actions"
///     IsContextual="True"
///     ContextualGroup="{StaticResource SelectionTools}"
///     IsActive="{x:Bind HasSelection, Mode=OneWay}" /&gt;
/// </code>
/// </example>
public partial class RibbonContextualGroup : DependencyObject
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonContextualGroup), new PropertyMetadata(string.Empty, OnChanged));

    /// <summary>Identifies the <see cref="Accent"/> dependency property.</summary>
    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(RibbonContextualGroup), new PropertyMetadata(null, OnChanged));

    /// <summary>Occurs when the name or the colour changes, so that a strip already drawing this heading redraws it.</summary>
    internal event EventHandler? Changed;

    /// <summary>Gets or sets the name on the band, already in the user's language.</summary>
    /// <remarks>
    /// Read out as part of what each tab of the group announces itself as, through
    /// <see cref="RibbonStrings.ContextualTabInGroupNameFormat"/>: the band says these tabs belong
    /// together only to somebody looking at the strip.
    /// </remarks>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets the colour of the band and of the tabs under it. The ribbon's accent colour when it is not set.</summary>
    /// <remarks>
    /// <para>
    /// One brush does the whole heading: the band behind the name, the tint behind the tabs of the
    /// group, and the line along the top of each of them. The band and the tint are drawn from it at
    /// <c>RibbonContextualTintOpacity</c>, so a saturated colour is what to hand over - it is the
    /// same colour the line is drawn in at full strength.
    /// </para>
    /// <para>
    /// A <c>ThemeResource</c> of the application's own is the way to make it follow the theme, which
    /// is why this is a brush and not a colour.
    /// </para>
    /// </remarks>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    private static void OnChanged(DependencyObject group, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonContextualGroup)group).Changed?.Invoke(group, EventArgs.Empty);
    }
}
