using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Ribbon;

/// <summary>A label and an icon around a control of your own.</summary>
/// <remarks>
/// <para>
/// Optional. A group takes any element at all, so a <c>NumberBox</c> can go straight into
/// <see cref="RibbonGroup.Items"/> with nothing wrapped around it and nothing declared - it is laid
/// out as <see cref="RibbonItemSize.Normal"/>, and the ribbon does not take the focus away from it.
/// This type is for when the control needs a name beside it, which is the usual reason to want one.
/// </para>
/// <para>
/// It accepts <see cref="RibbonItemSizes.Normal"/> alone by default, because a text field with its
/// label hidden is a text field nobody can identify. Say otherwise with
/// <see cref="Ribbon.SetAllowedSizes"/> if the control inside is worth shrinking.
/// </para>
/// </remarks>
public partial class RibbonContentItem : ContentControl, IRibbonItem
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonContentItem), new PropertyMetadata(string.Empty, OnLabelChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(Microsoft.UI.Xaml.Controls.IconSource), typeof(RibbonContentItem), new PropertyMetadata(null));

    private const string LabelPart = "PART_Label";

    private TextBlock? labelText;

    /// <summary>Initializes a new instance of the <see cref="RibbonContentItem"/> class.</summary>
    public RibbonContentItem()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonContentItem);
        Ribbon.SetAllowedSizes(this, RibbonItemSizes.Normal);

        // The host is chrome, not a stop on the way to the control it holds. This is the third of
        // the three reasons this library exists: a ribbon whose item host takes the focus is one
        // where every hosted NumberBox needs a flag set on it by hand before it can be typed into.
        IsTabStop = false;
    }

    /// <inheritdoc/>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc/>
    public IconSource? IconSource
    {
        get => (IconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        labelText = GetTemplateChild(LabelPart) as TextBlock;
        NameTheContent();
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        NameTheContent();
    }

    private static void OnLabelChanged(DependencyObject item, DependencyPropertyChangedEventArgs arguments)
    {
        var host = (RibbonContentItem)item;

        host.NameTheContent();
        host.InvalidateMeasure();
    }

    // The host is chrome; what a driver finds and a screen reader reads is the control inside it,
    // and a NumberBox dropped into a ribbon arrives with no name of its own at all. The probe found
    // that gap the first time it was asked, and this is the answer: the label beside the control is
    // what names it.
    //
    // Tied to the label rather than copied from it. A copy would go stale, and it would also be the
    // wrong thing to hand a screen reader, which wants to be told that this text labels that field
    // rather than hearing the same words twice.
    //
    // Never over an application that has already said something itself.
    private void NameTheContent()
    {
        if (labelText is null || Content is not UIElement content)
        {
            return;
        }

        if (string.IsNullOrEmpty(AutomationProperties.GetName(content)) && AutomationProperties.GetLabeledBy(content) is null)
        {
            AutomationProperties.SetLabeledBy(content, labelText);
        }
    }
}
