# The keyboard

The ribbon is one stop on the way through the window, and everything inside it is reached with the
arrow keys. That is WinUI's shape — a `CommandBar`, a `NavigationView`, a `TabView` and a `Pivot` all
work that way — and it is close enough to Office's that a hand which has used one is not surprised
by the other.

There is nothing to switch on and nothing to declare. A ribbon built in XAML or from code is
navigable as soon as it is on screen.

## In one screenful

| Key | Where the focus is | What happens |
| --- | --- | --- |
| `Tab` | anywhere in the window | Reaches the ribbon at the tab on show, and the next `Tab` leaves it. |
| `←` `→` | on the tab strip | The tab beside it, and the ribbon changes with it. Wraps round the ends. |
| `Home` `End` | on the tab strip | The first and the last tab on the strip. |
| `↓` | on the tab strip | Into the commands, at the first one. A minimised ribbon opens the tab over the content first. |
| `↑` | in the top row | Back to the tab strip. |
| `←` `→` `↑` `↓` | on a command | The nearest command that way, across groups and into a folded group's button. |
| `Space` `Enter` | on a command | Presses it. A drop-down opens; `Esc` closes it again. |
| `Esc` | on a command | Back to the tab strip, and a tab opened over the content closes. |
| `Esc` | on the tab strip | Out of the ribbon, back where the focus came from. |
| `Ctrl+F1` | anywhere in the window | What the chevron does, which is [`CollapseBehavior`](../README.md#features). |

## One stop, and it is the tab on show

Tab reaches the ribbon once. The stop it reaches is the header of the tab on show, whichever way
round the window Tab was going, and the next press leaves the ribbon for whatever is under it.

That is the whole reason the arrow keys exist here. A ribbon that made every command a tab stop would
be a ribbon nobody tabs past twice: forty presses to get from the address bar to the page. Office
takes the other road — inside its ribbon `Tab` walks every control — and it can, because you enter an
Office ribbon deliberately with `F6` or `Alt` and leave it with `Esc`. A control that has to sit in
somebody else's window cannot assume either.

The mechanism is worth knowing if you are retemplating: the header of the tab on show is the only
header that is a tab stop at all, and the ribbon hands that over as the selection moves. If the focus
is on the strip when the tab changes — because an arrow key changed it, or because a contextual tab
arrived — the focus goes with it.

## The arrows between commands

Left, right, up and down between commands are WinUI's XY focus navigation, which the ribbon switches
on for itself and everything in it. It reads where things ended up on screen rather than a list kept
by this control, which is what makes it right at every width: an item that stepped down to its icon,
a group that folded into a button, a hosted `ComboBox` two rows tall — all of them are just
rectangles to it, and the nearest rectangle that way is the answer.

Nothing about that is configurable here. `XYFocusUp` and its three companions are ordinary WinUI
properties, so an application that wants a particular command to be reached from a particular
direction can say so on the command itself.

## Esc, and the one place the arrows are not the ribbon's

A control hosted in a group keeps the keys it needs. A `NumberBox` takes the left and right arrows
for its caret and the up and down arrows for its value, which is exactly right and exactly what makes
it a trap: an arrow key will not carry you out of it.

`Esc` will. It puts the focus back on the tab strip from anywhere under it, and from the strip it
goes back to wherever the focus came into the ribbon from — the page, usually. That is two presses
from a text box in a group to the document, and it is the same two presses in Office.

`Esc` also closes what is open: a drop-down closes on the first press without leaving the command it
belongs to, and a tab that a minimised ribbon has opened over the content closes with the focus
landing back on the strip rather than wherever the window happens to keep its first button.

## What a screen reader is told

Everything the keyboard reaches has a name and answers to the pattern its kind implies — that is
described in the README, and it is the same surface a test driver uses. Two things are worth adding
here:

- A tab that is off the strip is out of the automation tree, not merely unpressable. A driver that
  can find a tab it cannot press will one day wait forever for the command behind it.
- The ribbon answers as a `Tab` with a `Selection` pattern, and each header as a `TabItem` with
  `SelectionItem`, so which tab is showing is a question that can be asked and answered from outside
  the process — including while the arrow keys are moving it about.

## What is not here

**Keytips.** `Alt` followed by a letter per tab and per command, as Office has. Not in this version;
the shape is left open for it.

**`F6`.** Office cycles panes with it. A ribbon in somebody else's window does not own the cycle, and
`Tab` already reaches this one.

**Rebindable keys.** The arrows and `Esc` are what they are. `Ctrl+F1` is a `KeyboardAccelerator` on
the control, so an application that needs the shortcut for something else can remove it from
`KeyboardAccelerators` and call its own gesture instead.

## How it is tested

Which tab an arrow key moves to is arithmetic over which tabs are on the strip, and it is a unit
test — including the wrapping, the tabs it steps over, and the strip with nothing on it.

The rest is a measurement in the harness, which reads the two things Tab is decided by rather than
pressing anything: which elements are stops at all, and where the focus manager says the next one is.
It checks that exactly one header is a stop and that it is the tab on show, that the next stop after
it is outside the ribbon, that an arrow from a command finds another command and an arrow up finds
the strip, that the focus follows the tab as the selection moves, and that a tab closing over the
content leaves the focus on the strip.

A keystroke itself cannot be faked from inside the process, so `Esc` and the arrows are pressed by
hand. What can be asked without a keystroke is asked on every run.
