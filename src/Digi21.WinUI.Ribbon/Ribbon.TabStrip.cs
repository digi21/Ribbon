using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon;

// Where the tab names are, for an application that puts something of its own beside them.
//
// The usual something is a title bar: a window that extends its content into the title bar puts the
// ribbon's tabs up there, and anything else it puts up there - commands, a search box, the region
// the user drags the window by - has to start where the tabs end and has to stop the window from
// being made so narrow that it covers them. The strip panel cannot be asked that from outside: it is
// stretched to the width of the ribbon, so its size says how wide the ribbon is rather than how
// much of it the tabs take, and its SizeChanged is silent when a contextual tab arrives.
public partial class Ribbon
{
    private RibbonTabStripPanel? arrangedStrip;
    private XamlRoot? watchedRoot;
    private double scale;

    /// <summary>Occurs when <see cref="TabStripBounds"/> changes, and when the display scale does.</summary>
    /// <remarks>
    /// <para>
    /// Raised after the layout pass that moved the tabs, so the rectangle a handler reads is the one
    /// on screen: when a contextual tab arrives or leaves, when a tab is renamed - a change of
    /// language renames every one of them - when the ribbon is retemplated or laid out somewhere
    /// else, and when a contextual group's name widens the tabs under it.
    /// </para>
    /// <para>
    /// Also raised when the window moves to a display of another scale, even though the rectangle,
    /// being in effective pixels, usually reads the same: what it covers in physical pixels has
    /// changed, and that is the number a title bar's drag regions and a window's minimum size are
    /// set in.
    /// </para>
    /// <para>The argument is the new <see cref="TabStripBounds"/>, boxed.</para>
    /// </remarks>
    public event TypedEventHandler<Ribbon, object>? TabStripBoundsChanged;

    /// <summary>Gets the rectangle the tab names take, as they are drawn.</summary>
    /// <remarks>
    /// <para>
    /// In effective pixels, in the ribbon's own coordinate space - the one
    /// <c>TransformToVisual(ribbon)</c> maps into, so it is converted to window coordinates with
    /// <c>ribbon.TransformToVisual(null).TransformBounds(ribbon.TabStripBounds)</c>, and to physical
    /// pixels by multiplying by <c>XamlRoot.RasterizationScale</c>. In a ribbon laid out right to left
    /// that space is mirrored, as it is for every element in it, and <see cref="Rect.X"/> is measured
    /// from the ribbon's right edge.
    /// </para>
    /// <para>
    /// It is as wide as the tabs need, not as wide as the strip they sit in: the strip is stretched
    /// across the ribbon, and the room to its right is room an application can use. Only the tabs
    /// on the strip count - a contextual tab that is switched off takes nothing - and so does the
    /// coloured band over a set of contextual tabs, whose height is held whether or not a band is
    /// drawn. When the window is too narrow for the tabs it is still as wide as they need, which is
    /// what makes it the number to set a window's minimum width from.
    /// </para>
    /// <para>
    /// All zeros until the ribbon has been laid out, and while its strip is not on screen; kept up to
    /// date otherwise, and <see cref="TabStripBoundsChanged"/> says when it moves.
    /// </para>
    /// </remarks>
    public Rect TabStripBounds { get; private set; }

    // Called from the constructor, once.
    private void ConfigureTabStrip()
    {
        Loaded += (_, _) => Watch(XamlRoot);
        Unloaded += (_, _) => Watch(null);
    }

    // Called from OnApplyTemplate with whatever the template put under PART_TabStrip.
    private void AttachTabStrip(Panel? panel)
    {
        if (arrangedStrip is not null)
        {
            arrangedStrip.Arranged -= OnTabStripArranged;
        }

        arrangedStrip = panel as RibbonTabStripPanel;

        if (arrangedStrip is not null)
        {
            arrangedStrip.Arranged += OnTabStripArranged;
        }
    }

    private void OnTabStripArranged(object? sender, EventArgs arguments)
    {
        UpdateTabStripBounds(rescaled: false);
    }

    // The display scale lives on the XamlRoot, which a ribbon has only while it is in a window, and
    // which is a different one if it is moved to another.
    private void Watch(XamlRoot? root)
    {
        if (watchedRoot is not null)
        {
            watchedRoot.Changed -= OnXamlRootChanged;
        }

        watchedRoot = root;

        if (watchedRoot is not null)
        {
            watchedRoot.Changed += OnXamlRootChanged;
            scale = watchedRoot.RasterizationScale;
        }
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs arguments)
    {
        // Changed is raised for the size and the visibility of the window too, which say nothing
        // about the tabs: those reach this ribbon as a layout pass, if they reach it at all.
        if (sender.RasterizationScale == scale)
        {
            return;
        }

        scale = sender.RasterizationScale;
        UpdateTabStripBounds(rescaled: true);
    }

    // Reads where the tabs are and says so when that has moved.
    //
    // Called at the end of the strip's arrange, which is where a tab arriving or being renamed shows
    // up, and at the end of the ribbon's, which is where the strip being put somewhere else does: the
    // strip's own arrange runs before its parent has placed it, so on the pass that moves it the
    // first reading is from where it was and the second from where it went.
    private void UpdateTabStripBounds(bool rescaled)
    {
        Rect bounds = default;

        if (tabStrip is { XamlRoot: not null, Visibility: Visibility.Visible } panel)
        {
            Point origin = panel.TransformToVisual(this).TransformPoint(default);

            // The size it asked for rather than the one it was given, which is the whole point: the
            // panel is given the width of the ribbon and asks for the width of its tabs. Asked of the
            // panel itself where it is this library's, because DesiredSize is clipped to the room on
            // offer; a template that put some other panel there gets the clipped answer.
            Size natural = arrangedStrip?.Natural ?? Requested(panel);

            bounds = new Rect(origin.X, origin.Y, natural.Width, natural.Height);
        }

        if (bounds == TabStripBounds && !rescaled)
        {
            return;
        }

        TabStripBounds = bounds;
        TabStripBoundsChanged?.Invoke(this, bounds);
    }

    // What a panel asked for, without its margin, which DesiredSize includes and the panel's own
    // origin does not.
    private static Size Requested(FrameworkElement panel)
    {
        Thickness margin = panel.Margin;

        return new Size(
            Math.Max(0, panel.DesiredSize.Width - margin.Left - margin.Right),
            Math.Max(0, panel.DesiredSize.Height - margin.Top - margin.Bottom));
    }
}
