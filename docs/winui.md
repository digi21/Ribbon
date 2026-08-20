# What WinUI cost this library

Things that are not guessable from the documentation, that fail quietly, and that each cost an
afternoon here. Read it before concluding that something is impossible, and add to it when you find
the next one.

## Deriving from one of WinUI's newer controls loses the template

`RibbonDropDownButton` derives from `DropDownButton` and set `DefaultStyleKey` to its own type, as
every other item does. It got **no template at all**: no visual children, a desired size of zero, and
an arrangement into a column of zero width. Present according to every count, and invisible.

`Button` and `ToggleButton` are older controls and resolve their default style the classic way, by
looking up the type key in the application's resources — where `Themes/Generic.xaml` has put it.
`DropDownButton`, `SplitButton` and the rest of the newer set look theirs up in the dictionary
`DefaultStyleResourceUri` names, which inherited unchanged points at WinUI's own, where a type of
ours does not exist.

```csharp
DefaultStyleKey = typeof(RibbonDropDownButton);
DefaultStyleResourceUri = new Uri("ms-appx:///Digi21.WinUI.Ribbon/Themes/Generic.xaml");
```

Nothing warns about it. If you add an item deriving from one of the newer controls, set both, and
check that it renders — the probe now fails when an item on the strip has no size, which is what
caught this.

## A layer brush has nothing behind it in a popup

The ribbon paints with `LayerFillColorDefaultBrush`, as WinUI's own surfaces do, and layer brushes
are translucent by design: they are meant to sit on a page. The same brush on the popup a minimised
ribbon opens over the application's content shows the document through it.

Anything that floats needs an opaque surface of its own — `RibbonOverlayBackgroundBrush` here. The
same applies to the application: give the root of your window a background or an unpackaged WinUI
window shows through black, which looks right by accident in a dark theme and turns every word
invisible in a light one.

## In-process automation peers are not the automation surface

WinUI implements the pattern providers of its built-in peers natively, so `TextBoxAutomationPeer`
answers no managed `IValueProvider` even though a UIA client outside the process sees a perfectly
good `ValuePattern` on it. Typing into a control through its own peer, from inside the application,
does not work.

`ButtonBase` also brings no automation peer of its own, unlike `Button`. A tab header derived from it
answered to no pattern at all until `RibbonTabHeaderAutomationPeer` was written, which meant every
tab was unreachable except by clicking a coordinate.

## A collapsed element has no peer at all, not a peer that says it is hidden

`FrameworkElementAutomationPeer.CreatePeerForElement` returns **null** for an element whose
`Visibility` is `Collapsed`, rather than a peer reporting `IsOffscreen`. That is what makes
`Visibility.Collapsed` the right way to take a contextual tab off the strip: the header is kept — the
same object, for when the tab comes back — and UI Automation stops admitting to it, so a driver
cannot find a tab it would not be able to press.

It cuts the other way for any code that sweeps a visual tree and asks each element for its name and
patterns. The probe's `UIA` scenario did exactly that and started failing the day a tab could be off
the strip: it was asking a tab that is not there to answer for itself. A sweep like that has to
filter on `Visibility` and say so, because the alternative — a sweep that silently skips whatever has
no peer — is one that quietly stops covering things.

## A resize is delivered before the layout it causes

`AppWindow.Resize` hands the island its new size and returns; the measure and arrange it causes are
queued behind that. Reading a width back on the next turn of the dispatcher reads the width from
before the resize, and timing from the call to the pass that follows measures the frame cadence
rather than the layout.

Both of those produced numbers that looked entirely reasonable — a whole sweep shifted by one step,
and a median of 16.7 ms on a sixty hertz screen, which is 1000/60.

## `DesiredSize` is clamped to what you measured with

An element that wants more room than it was offered reports exactly what it was offered. A strip
overflowing its window cannot be detected by asking it how wide it wants to be; where the overflow is
visible is the far side of the last thing arranged, which an arrange puts wherever the widths add up
to.
