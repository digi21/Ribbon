using Digi21.WinUI.Ribbon.Layout;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Digi21.WinUI.Ribbon;

// The keyboard: where the focus comes into the ribbon, how it moves about inside it, and how it
// leaves again. Its own file because it is one subject and it is answerable as one - the rest of
// the control is about what the ribbon draws at a width, and none of this is.
//
// The shape of it is WinUI's rather than Office's, on purpose, and it is the shape of every WinUI
// control that holds a strip of anything - CommandBar, NavigationView, TabView, Pivot:
//
// - The whole ribbon is one stop on the way through the window. Tab reaches it and Tab leaves it,
//   and where it lands is the tab on show. A ribbon whose Tab key walked forty commands before it
//   reached the page would be a ribbon nobody ever tabbed past twice, and Office only gets away
//   with walking them because you enter its ribbon with F6 or Alt and leave it with Esc.
// - Inside, the arrow keys move. Left and right along the strip of names, which changes tab as it
//   goes because a tab strip that moved the focus without moving the ribbon would be a strip you
//   have to press a second key to use; down into the body; and then the arrows again between the
//   commands, where the ribbon has nothing to say - it hands that to XY focus navigation, which
//   already knows how to find the nearest thing in a direction and does it from the geometry the
//   layout has just settled on rather than from a list this control would have to keep in step.
// - Esc goes back the way it came: out of the body to the strip, and out of the strip to whatever
//   the user was doing before. It is also the way out of a hosted control, which is the one place
//   in a ribbon where the arrow keys belong to somebody else.
public partial class Ribbon
{
    // Where the focus was standing before it came in here, for the Esc that sends it back. Weak,
    // because a ribbon has no business keeping a page alive that the window has moved on from, and
    // because the answer to a page that has gone is the same as the answer to never having had one.
    private WeakReference<UIElement>? outside;

    // What the focus is on, or nothing when this ribbon is not in a window yet.
    private DependencyObject? Focused =>
        XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        ArgumentNullException.ThrowIfNull(e);

        if (e.Handled)
        {
            return;
        }

        // Where the focus is standing decides which half of the ribbon the key belongs to: the
        // strip of names along the top, or everything under it. A key this ribbon does not want is
        // left alone rather than swallowed, which is what leaves the arrows between commands to XY
        // navigation and Space and Enter to the button they land on.
        e.Handled = Ancestor<RibbonTabHeader>(e.OriginalSource as DependencyObject) is { } header
            ? OnStripKey(header, e.Key)
            : OnBodyKey(e.Key);
    }

    // Whether an element is inside another, walked through the visual tree rather than the logical
    // one - which is what keeps the body inside the ribbon while it is sitting in the popup a
    // minimised ribbon opens a tab in.
    private static bool Contains(DependencyObject scope, DependencyObject? element)
    {
        for (DependencyObject? walk = element; walk is not null; walk = VisualTreeHelper.GetParent(walk))
        {
            if (ReferenceEquals(walk, scope))
            {
                return true;
            }
        }

        return false;
    }

    private static T? Ancestor<T>(DependencyObject? element)
        where T : DependencyObject
    {
        for (DependencyObject? walk = element; walk is not null; walk = VisualTreeHelper.GetParent(walk))
        {
            if (walk is T found)
            {
                return found;
            }
        }

        return null;
    }

    // The first command of a tab, whichever group it is in and whatever shape it ended up in. Asked
    // of WinUI rather than worked out here: a group that has folded holds its items in a closed
    // flyout, an item drawn at a width that hides it is not focusable, and the one place that knows
    // all of that already is the focus manager.
    private static bool FocusFirst(RibbonTab tab) =>
        FocusManager.FindFirstFocusableElement(tab) is UIElement first && first.Focus(FocusState.Keyboard);

    // Called from the constructor, once, before there is a template or a tab.
    private void ConfigureKeyboard()
    {
        // One stop for the whole ribbon on the way through the window. Which element that stop is
        // does not depend on this: it is the header of the tab on show, and the strip keeps it so
        // by being the only header that is a tab stop at all.
        TabFocusNavigation = KeyboardNavigationMode.Once;

        // And the arrows for everything inside it. Enabled here and inherited by everything below,
        // so a command finds the command beside it, a folded group's button finds the group before
        // it, and the top row of the body finds the strip above it - all of it from where things
        // actually ended up on screen, which is the only description of a ribbon's contents that is
        // true at every width.
        XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

        GettingFocus += OnGettingFocus;
    }

    private void OnGettingFocus(UIElement sender, GettingFocusEventArgs arguments)
    {
        // Tab, and only Tab. An arrow key, a click and an application calling Focus all know where
        // they are going, and this is about the one way in that does not.
        if (arguments.Direction is not (FocusNavigationDirection.Next or FocusNavigationDirection.Previous))
        {
            return;
        }

        // Focus moving about inside the ribbon is not focus arriving at it. The body is asked about
        // separately from the ribbon because a minimised ribbon keeps it in a popup, and a popup is
        // a tree of its own: walked upwards from a command in there, the ribbon is not an ancestor
        // and the body always is.
        if (arguments.OldFocusedElement is DependencyObject inside
            && (Contains(this, inside) || (body is not null && Contains(body, inside))))
        {
            return;
        }

        if (arguments.OldFocusedElement is UIElement from)
        {
            outside = new WeakReference<UIElement>(from);
        }

        if (arguments.InputDevice != FocusInputDeviceKind.Keyboard)
        {
            return;
        }

        // One door in, whichever way Tab was going through the window. Without this, tabbing
        // forwards arrives at the front of the ribbon and tabbing backwards at the back of it - some
        // command in the middle of the last group - and the ribbon answers one key with two places.
        if (SelectedHeader is { } header && !ReferenceEquals(arguments.NewFocusedElement, header))
        {
            arguments.TrySetNewFocusedElement(header);
        }
    }

    private bool OnStripKey(RibbonTabHeader header, VirtualKey key)
    {
        switch (key)
        {
            case VirtualKey.Left:
            case VirtualKey.Right:

                // Forwards is the way the reading goes and not the way the screen does: a ribbon in
                // a window laid out right to left moves the other way for the same key, as
                // everything else in that window does.
                bool forward = (key == VirtualKey.Right) != (FlowDirection == FlowDirection.RightToLeft);

                return Move(RibbonKeyboard.Step(
                    Activity(),
                    header.Tab is { } tab ? tabs.IndexOf(tab) : -1,
                    forward));

            case VirtualKey.Home:
                return Move(RibbonKeyboard.Edge(Activity(), first: true));

            case VirtualKey.End:
                return Move(RibbonKeyboard.Edge(Activity(), first: false));

            case VirtualKey.Down:
                return EnterBody();

            case VirtualKey.Escape:
                return LeaveRibbon();

            default:
                return false;
        }
    }

    private bool OnBodyKey(VirtualKey key) => key == VirtualKey.Escape && LeaveBody();

    // Esc under the strip, wherever the body happens to be hanging at the time.
    //
    // Hooked to the body itself and not to the ribbon, which is not a detail: a minimised ribbon
    // keeps its body in a popup, and a popup is a tree of its own - a key pressed on a command in
    // there routes up as far as the body and no further, so a ribbon listening only to itself hears
    // nothing at all from the one place this key matters most.
    private void OnBodyKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!e.Handled && e.Key == VirtualKey.Escape)
        {
            e.Handled = LeaveBody();
        }
    }

    // Esc is the way back to the strip, and it is the only way out of a hosted control: a text box
    // keeps the arrow keys for its caret, so somebody who has arrowed into a NumberBox and wants to
    // carry on along the row has to be able to say so with a key the box does not want. Office
    // answers Esc here and so does this.
    //
    // It also puts a tab away that was opened over the content, because a ribbon that is minimised
    // is one the user has already asked to stop looking at.
    private bool LeaveBody()
    {
        bool moved = FocusStrip();

        CloseOverlay();

        return moved;
    }

    // Closes the tab a minimised ribbon opened over the content, and moves the focus out of it
    // first.
    //
    // A popup that goes away under the focus drops it, and the window then hands it to the first
    // thing it can find - which is whatever button the application's page happens to begin with, and
    // never the ribbon. Nothing can be done about that afterwards: by the time the popup says it has
    // closed the focus is already nowhere, and nowhere does not say where it had been. So it is done
    // beforehand, and every way this control closes that tab comes through here.
    //
    // The one way out that does not is the popup's own light dismissal, which is a click somewhere
    // else. It does not need to: that click is on its way to whatever was clicked, and that is where
    // the focus belongs.
    private void CloseOverlay()
    {
        if (overlay is not { IsOpen: true })
        {
            return;
        }

        if (body is not null && Contains(body, Focused))
        {
            FocusStrip();
        }

        overlay.IsOpen = false;
    }

    // Asks for a tab by index, from the keyboard. The selection is what moves the focus, not this:
    // the header of the tab arriving is the strip's only tab stop, and MarkHeaders puts the focus on
    // it as it hands the stop over.
    private bool Move(int index)
    {
        if (index < 0)
        {
            return false;
        }

        if (index != SelectedIndex)
        {
            SelectedIndex = index;
        }

        // Consumed even when it moved nowhere - a strip of one tab, or an end that wrapped onto
        // itself. Letting the key fall through from here would hand the focus sideways out of the
        // strip and into the body, which is not what an arrow along a row of names means.
        return true;
    }

    // Down, from the strip into the commands under it.
    private bool EnterBody()
    {
        if (SelectedTab is not { } tab)
        {
            return false;
        }

        // A minimised ribbon has no body on screen to walk into. The tab opens over the content
        // exactly as it does for a click, and the focus goes into what has just opened - which is
        // what the key asked for, and the only reading of it that does anything at all.
        if (IsMinimized && overlay is not { IsOpen: true })
        {
            ShowOverlay();
        }

        if (!FocusFirst(tab))
        {
            // The popup has only just been told to open and what is in it has not been laid out, so
            // there is nothing focusable in there yet. The key is still the ribbon's - it has
            // already moved - and the focus follows a frame later rather than not at all.
            DispatcherQueue.TryEnqueue(() => FocusFirst(tab));
        }

        return true;
    }

    private bool FocusStrip() => SelectedHeader is { } header && header.Focus(FocusState.Keyboard);

    // Esc, from the strip, which is as far back as the ribbon goes.
    private bool LeaveRibbon()
    {
        // Back where the user came from. Esc in a ribbon means "not this", and leaving somebody
        // standing on a strip of tab names they have just dismissed is not that.
        if (outside is not null
            && outside.TryGetTarget(out UIElement? element)
            && element.XamlRoot is not null
            && element.Focus(FocusState.Keyboard))
        {
            return true;
        }

        // Nobody came in from anywhere - the application put the focus here, or the page it came
        // from has gone since. The focus is handed on to whatever follows the ribbon, which is the
        // journey Tab would have made and lands in the same place.
        return FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
    }

    // Which header is the tab on show, and which one the keyboard is allowed to stand on.
    //
    // One tab stop for the whole strip and it is the tab on show, which is what makes the ribbon one
    // stop on the way through a window: the other headers are reached with the arrow keys, which is
    // where the tab strip of a WinUI control keeps them too.
    private void MarkHeaders(RibbonTab? selected)
    {
        // Read before anything moves. A focus standing on the strip has to go on standing on it,
        // on the header of whatever tab has just arrived; a focus anywhere else is not this
        // method's business, and a tab changed by an application while the user is typing in the
        // page must not take the focus off what they are typing into.
        FocusState standing = FocusState.Unfocused;

        foreach (RibbonTabHeader header in headers)
        {
            if (header.FocusState != FocusState.Unfocused)
            {
                standing = header.FocusState;
            }
        }

        RibbonTabHeader? chosen = null;

        foreach (RibbonTabHeader header in headers)
        {
            header.IsSelected = ReferenceEquals(header.Tab, selected);

            if (!header.IsSelected)
            {
                continue;
            }

            // Made a stop before the focus is moved onto it, and before the old one stops being
            // one: an element that is not a tab stop cannot be focused, so the order here is the
            // difference between the focus arriving and the focus being dropped on the floor.
            header.IsTabStop = true;
            chosen = header;
        }

        if (standing != FocusState.Unfocused)
        {
            // In the state it was in, so that a strip somebody arrowed along goes on drawing the
            // focus and one an application moved under them does not start.
            chosen?.Focus(standing);
        }

        foreach (RibbonTabHeader header in headers)
        {
            if (!header.IsSelected)
            {
                header.IsTabStop = false;
            }
        }
    }
}
