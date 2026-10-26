using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Digi21.WinUI.Ribbon;

/// <summary>An item in a group with two halves: a command, and beside it a menu of related ones.</summary>
/// <remarks>
/// <para>
/// A <see cref="SplitButton"/>, so <c>Command</c>, <c>Click</c>, <c>Flyout</c> and the automation
/// peer come from WinUI - <c>InvokePattern</c> for the command and <c>ExpandCollapsePattern</c> for
/// the menu, which is what a driver needs to tell the two halves apart.
/// </para>
/// <para>
/// The menu is a <see cref="Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase"/>, as it is on
/// <see cref="RibbonDropDownButton"/>: usually a <see cref="MenuFlyout"/> of
/// <see cref="MenuFlyoutItem"/>s, each with its own <c>Text</c>, <c>Icon</c> and <c>Command</c>, and
/// each greyed out by its own command's <c>CanExecute</c>.
/// </para>
/// <para>
/// Clicking the command half, or pressing Enter or Space on the button, runs the command. Clicking
/// the chevron, Alt+Down or Down opens the menu, and Esc closes it with the focus back on the button.
/// <see cref="Control.IsEnabled"/> greys out both halves; the command's <c>CanExecute</c> greys out
/// only the command half, and the menu stays available.
/// </para>
/// <para>
/// Drawn Large, the icon is the command and the name with the chevron under it opens the menu, as in
/// Office. Drawn Normal or Small, the chevron at the end opens the menu and the rest is the command.
/// </para>
/// </remarks>
public partial class RibbonSplitButton : SplitButton, IRibbonItem
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonSplitButton), new PropertyMetadata(string.Empty, OnChromeChanged));

    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(Microsoft.UI.Xaml.Controls.IconSource), typeof(RibbonSplitButton), new PropertyMetadata(null, OnChromeChanged));

    private readonly RibbonItemChrome chrome;

    private RibbonItemContent? content;
    private Button? primary;
    private Button? secondary;

    /// <summary>Initializes a new instance of the <see cref="RibbonSplitButton"/> class.</summary>
    public RibbonSplitButton()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonSplitButton);

        // SplitButton is one of WinUI's newer controls, like DropDownButton, and looks its default
        // style up where this names rather than in the application's resources. See
        // RibbonDropDownButton for what happens without it.
        DefaultStyleResourceUri = new Uri("ms-appx:///Digi21.WinUI.Ribbon/Themes/Generic.xaml");
        chrome = new RibbonItemChrome(this, () => Label, () => IconSource);
        Ribbon.SetAllowedSizes(this, RibbonItemSizes.All);
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
        if (primary is not null)
        {
            primary.IsEnabledChanged -= OnHalfEnabledChanged;
        }

        if (secondary is not null)
        {
            secondary.IsEnabledChanged -= OnHalfEnabledChanged;
        }

        base.OnApplyTemplate();

        content = GetTemplateChild("PART_Content") as RibbonItemContent;
        primary = GetTemplateChild("PrimaryButton") as Button;
        secondary = GetTemplateChild("SecondaryButton") as Button;

        // Each half greys itself out from its own IsEnabled: the command half's follows its
        // command's CanExecute, because the template binds the command to it, and both follow the
        // button's own IsEnabled, because a disabled control disables what is inside it.
        if (primary is not null)
        {
            primary.IsEnabledChanged += OnHalfEnabledChanged;
        }

        if (secondary is not null)
        {
            secondary.IsEnabledChanged += OnHalfEnabledChanged;
        }

        chrome.Attach(content);
        UpdateDimming();
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // Before the template is arranged, from the size the item has just been given: the halves
        // cover the room the item got, which in a column of wider items is more than it asked for.
        PlaceHalves(finalSize);

        return base.ArrangeOverride(finalSize);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // SplitButton runs the command on Enter and Space whatever the command says about it - it
        // raises Click and only then asks CanExecute - so a command half that is greyed out would
        // still answer the keyboard. Taken here, before it can.
        if (IsCommandKey(e.Key) && primary is { IsEnabled: false })
        {
            e.Handled = true;
            return;
        }

        // Down on its own, not only Alt+Down: in a ribbon the arrow below a button that opens
        // something is the one people press to open it. Opened on the way down, as a menu bar does.
        if (e.Key == VirtualKey.Down && IsEnabled && Flyout is not null)
        {
            e.Handled = Open();
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // The other half of each key taken above. Down has already opened the menu, and handing its
        // release on to SplitButton - which opens on the release of Alt+Down - would open it again.
        if ((IsCommandKey(e.Key) && primary is { IsEnabled: false }) || (e.Key == VirtualKey.Down && Flyout is not null))
        {
            e.Handled = true;
            return;
        }

        base.OnKeyUp(e);
    }

    private static bool IsCommandKey(VirtualKey key) =>
        key is VirtualKey.Enter or VirtualKey.Space or VirtualKey.GamepadA;

    private static void OnChromeChanged(DependencyObject item, DependencyPropertyChangedEventArgs arguments)
    {
        ((RibbonSplitButton)item).chrome.Update();
    }

    // Opens the menu the way a driver would, through the button's own ExpandCollapsePattern, which is
    // SplitButton's door to its private OpenFlyout: the menu is placed, and the open state drawn,
    // exactly as a click on the chevron would.
    private bool Open()
    {
        if ((FrameworkElementAutomationPeer.FromElement(this) ?? FrameworkElementAutomationPeer.CreatePeerForElement(this))
            is IExpandCollapseProvider menu)
        {
            menu.Expand();
            return true;
        }

        return false;
    }

    private void OnHalfEnabledChanged(object sender, DependencyPropertyChangedEventArgs arguments)
    {
        UpdateDimming();
    }

    private void UpdateDimming()
    {
        content?.Dim(primary is { IsEnabled: false }, secondary is { IsEnabled: false });
    }

    // Puts the two halves where the content says they meet. In a column under the icon when the item
    // is drawn Large, and at the end of the row otherwise.
    //
    // The content covers both halves, so moving the line between them changes nothing it measures:
    // the pass this causes arranges the two halves again and stops there.
    private void PlaceHalves(Size size)
    {
        if (content is null || primary is null || secondary is null)
        {
            return;
        }

        bool large = content.ItemSize == RibbonItemSize.Large;
        double extent = Math.Min(Math.Round(content.SecondaryExtent(size)), large ? size.Height : size.Width);

        Place(primary, large ? new Rect(0, 0, size.Width, size.Height - extent) : new Rect(0, 0, size.Width - extent, size.Height));
        Place(secondary, large ? new Rect(0, size.Height - extent, size.Width, extent) : new Rect(size.Width - extent, 0, extent, size.Height));
    }

    // Only what has moved: these are layout properties, and writing the same value on every pass
    // would make every pass the cause of another.
    private static void Place(FrameworkElement half, Rect where)
    {
        if (Canvas.GetLeft(half) != where.X)
        {
            Canvas.SetLeft(half, where.X);
        }

        if (Canvas.GetTop(half) != where.Y)
        {
            Canvas.SetTop(half, where.Y);
        }

        if (half.Width != where.Width)
        {
            half.Width = where.Width;
        }

        if (half.Height != where.Height)
        {
            half.Height = where.Height;
        }
    }
}
