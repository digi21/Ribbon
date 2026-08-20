# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Nothing has been released yet. The repository builds as `0.1.0-dev.N` until the first `v` tag.

### Added

- The keyboard. The whole ribbon is one stop on the way through the window - `Tab` reaches it at the
  header of the tab on show, whichever way round it was going, and the next `Tab` leaves it - and
  everything inside it is reached with the arrow keys, which is the shape of every WinUI control that
  holds a strip of anything. Left and right move along the tabs and change the ribbon as they go,
  wrapping round the ends and stepping over contextual tabs that are switched off; `Home` and `End`
  go to either end of the strip; down goes into the commands, opening the tab over the content first
  when the ribbon is minimised; and the arrows between commands are XY focus navigation, which reads
  the geometry the layout has just settled on rather than a list this control would have to keep in
  step - so an item that has stepped down to its icon, a group that has folded into a button and a
  hosted control two rows tall are all just rectangles, and the nearest one that way is the answer.
  `Esc` comes back out: to the strip from a command, and out of the ribbon from the strip, back to
  whatever the focus came in from. It is also the only way out of a hosted control, because a
  `NumberBox` takes the arrow keys for its caret and its value and is right to - which makes it a
  trap no arrow key can leave. Office walks every command with `Tab` instead, and can, because you
  enter an Office ribbon deliberately with `F6` or `Alt`; a control that has to sit in somebody
  else's window cannot assume either, and forty tab stops between the address bar and the page is a
  ribbon nobody tabs past twice.
- The focus, drawn. These templates are the library's own, and a template that does not ask for the
  system focus visual does not get one: every place an arrow key puts somebody was moving a focus
  nobody could see, which is not navigation but guessing.
- A tab that a minimised ribbon opened over the content puts the focus back on the strip as it
  closes, rather than letting it fall wherever the window keeps its first button. The popup carrying
  the tab drops the focus on its way out and there is nothing to put back afterwards - by the time it
  says it has closed, the focus is already nowhere - so every way this control closes that tab now
  moves the focus out of it first.
- `Ribbon.TabTransition`: the change from one tab to the next is drawn rather than cut. The tab
  arriving fades in from the side the user moved towards, over 160 ms, because a tab is a whole strip
  of commands replaced at once and replacing it between two frames leaves the eye to work out on its
  own that everything under the strip is now something else. It is chrome over a change that has
  already happened - the tab is chosen, laid out and hit-testable before the first frame of it is
  drawn - and what moves is a render transform and an opacity, neither of which the layout system can
  see, so the ribbon is exactly as tall throughout as it was before and nothing is measured twice.
  `Fade` drops the movement, `None` draws nothing; a system told to show no animations is obeyed
  whatever the property says, and a minimised ribbon opening a tab over the content cuts, because the
  popup that carries it arrives with an animation of its own. A change of tab landing on top of one
  still being drawn stops it, so no tab is ever left standing where a transition pushed it.
- `RibbonTab.IsContextual`, `IsActive` and `SelectsWhenActivated`: a tab that is on the strip only
  while it is worth having, which is Office's contextual tab. Declared once with its groups and
  driven from then on by one ordinary two-way property, because the alternative it replaces - a fixed
  tab whose commands are switched off most of the time - never says *when* they will work, and a
  greyed-out button certainly never says that the moment has just arrived. It appears where it was
  declared rather than being moved to the end, so the visual order and `Tabs` stay the same thing; it
  steps forward as it arrives unless told not to; and when it goes it puts the user back on the tab
  they came from, or on the first tab there is when that one has gone too. It never changes the
  ribbon's height - every tab pays into the single height, the ones switched off included, because a
  ribbon that grew as a tab arrived would push the window down at the moment somebody was reaching
  into it - and nothing is rebuilt on the way, so a tab that comes and goes twenty times a minute
  costs one build. A simplified ribbon lays it out in one row like any other tab; a minimised ribbon
  shows its header without opening itself over the content, because a user who put the ribbon away
  asked for the content.
- `Ribbon.TabActivated` and `Ribbon.TabDeactivated`, raised after the strip has been rebuilt and
  after any move to the new tab, so a handler asking what is showing is told where the ribbon ended
  up rather than where it was on the way.
- `RibbonAutomationPeer`: the ribbon answers to `Tab` with `SelectionPattern`, and a tab header's
  `SelectionContainer` now names it instead of being null. The headers have called themselves tab
  items since the probe found them answering to no pattern at all; a tab item with no set above it
  was half an answer, and it stopped being enough when the set of tabs became something that changes
  while the application runs. A tab that is off the strip has a collapsed header and is therefore out
  of the automation tree altogether - unfindable rather than findable and unpressable - and a tab
  arriving raises `StructureChanged` on the ribbon, which is the same news for a driver out of
  process that `TabActivated` is for one in it.
- `RibbonStrings.ContextualTabNameFormat`, in nine languages: what a screen reader is told a
  contextual tab is. The accent line above it says it is one only to somebody looking at the strip,
  and what makes a contextual tab worth having is that it was not there a moment ago.
- Renaming a tab renames its header. `RibbonTab.Label` was read once, when the strip was built, so a
  tab renamed after that kept the name it was born with - which nothing had noticed because nothing
  renamed a tab. Contextual tabs needed the same path for `IsContextual`, and the label came with it.
- `RibbonContextualTabAccentBrush` and `RibbonContextualTabAccentHeight`: the line a contextual tab
  wears. Along the top edge, because the bottom one marks the tab on show and a contextual tab that
  is also the one on show has to say both things at once, and edge to edge rather than inset so that
  two of them side by side draw one unbroken line - which is where the coloured heading over a set of
  them goes, the day there is one.
- The control: `Ribbon`, `RibbonTab`, `RibbonGroup`, `RibbonButton`, `RibbonToggleButton`,
  `RibbonDropDownButton`, `RibbonContentItem` and `RibbonSeparator`, with `IRibbonItem` as what the
  item types share. Each derives from the WinUI control that already behaves correctly — a
  `Button`, a `ToggleButton`, a `DropDownButton` — so `InvokePattern`, `TogglePattern`, commands and
  keyboard activation are WinUI's rather than this library's. A group takes any `UIElement` at all,
  so a `NumberBox` needs no wrapper and keeps its focus.
- `Ribbon.AllowedSizes`, attached, for declaring which shapes an item accepts, and `Ribbon.GetSize`
  for reading the one it was given.
- `RibbonGroup.HasLauncher`, `LauncherFlyout` and `LauncherClick`: the small button beside a group's
  name that opens whatever the group has no room for. Off out of the box, and it goes with the name
  when the group folds - a folded group draws its button and nothing else.
- `Ribbon.DisplayMode` and `RibbonDisplayMode`: `Full`, three rows to a group as in Office, and
  `Simplified`, the whole strip in one row with the group names off and every item beside its label
  or down to its icon. It is the same walk through the same states with one row instead of three and
  the tallest shape never offered, so what does not fit folds into the group's button as it always
  did rather than into a second overflow mechanism. A group holding something that cannot be drawn
  in one row - a stack of labelled controls, or an item accepting no shape below `Large` - is its
  button at every width, and is laid out inside the flyout the way a full ribbon would lay it out.
  Independent of `IsMinimized`, and nothing is rebuilt on the way: the control an application put in
  a group is the same object in both modes.
- `Ribbon.CollapseBehavior` and `RibbonCollapseBehavior`: what the chevron, a double-click on a tab
  and `Ctrl+F1` do. One gesture with one meaning, chosen by the application - `Simplify`, the
  default, drops the ribbon to one row and brings it back; `Minimize` is the Office behaviour of
  putting it away; `None` takes the chevron off and leaves the ribbon where it is. Simplifying is
  the default because a chevron in a corner is easy to press by accident, and pressing one should
  not leave somebody in front of a window with no commands in it.
- `Ribbon.IsMinimized`, with a chevron in the tab strip, a double-click on a tab and `Ctrl+F1`.
  Minimised, the ribbon leaves only its tabs and gives the room back to the window; clicking a tab
  then opens it *over* the content, transiently, without changing the property — so an application
  saving `IsMinimized` records what the user asked for rather than what they last looked at.
- `RibbonStrings`, the few sentences the ribbon says on its own account: what a group's launcher is
  called, what the button a folded group becomes is called, and the four the chevron uses - two for
  a ribbon that simplifies when it is collapsed and two for one that minimises, because a button
  announcing that it minimises a ribbon it is about to simplify is worse than one that says nothing.
- `RibbonTabHeaderAutomationPeer`, so that a tab announces itself as a tab and can be chosen by
  something other than a click on a coordinate.
- A group is never narrower than its own name, as in Office, so the name under a group stays
  readable however hard the ribbon is squeezed.
- A row is as tall as the tallest item that sits in one - a standard control, or one with a name
  beside it - and anything taller spans as many rows as it needs of the three a group has, with
  those rows tall enough between them to hold it. A `ToggleSwitch` is therefore two rows rather than
  a tab of forty-pixel rows, and a stack of three labelled controls handed over as a single element
  is a column three rows deep: the ribbon is the height of the stack, once, not three times, and not
  two thirds of it with the first and last control cut off.
- One height for the whole ribbon: the tab that needs the most decides it and every tab is laid out
  at it, whatever width the window is. A ribbon is a strip with the whole window under it, and one
  that is taller for the tab holding a stack of controls than for the tab holding buttons moves
  everything below it each time a tab is chosen. The tabs that are not showing are asked what they
  need rather than measured, because a collapsed element measures as nothing however directly it is
  asked.
- A group applies its template before the layout reads anything off it, so that the first pass is as
  good as any later one. A control WinUI has never measured has no template - no name to put a floor
  under its group's width, and no parts to fold with - and a window that opens at the width it stays
  at gets that pass and no other: the strip was laid out on the understanding that a group had
  folded while the group drew itself open, and the difference went off the right-hand edge with a
  command on it.
- `RibbonItemSize` and `RibbonItemSizes`: the three shapes an item can take, and the set of them an
  item declares it accepts.
- The layout that decides between those shapes, ahead of the control that will use it. Items step
  down through the shapes they accept before any group gives up; the group with the lowest priority
  gives way first and ties are broken from the right; a group that no longer fits folds into a
  button instead of leaving the strip; and the last resort is those buttons dropping their labels,
  one at a time and in the same order. The arrangements are generated without reference to the width
  available, which only chooses which of them to stop at, and that is what keeps the result stable,
  reversible and free of flicker.
