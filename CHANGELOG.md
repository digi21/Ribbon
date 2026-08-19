# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Nothing has been released yet. The repository builds as `0.1.0-dev.N` until the first `v` tag.

### Added

- The control: `Ribbon`, `RibbonTab`, `RibbonGroup`, `RibbonButton`, `RibbonToggleButton`,
  `RibbonDropDownButton`, `RibbonContentItem` and `RibbonSeparator`, with `IRibbonItem` as what the
  item types share. Each derives from the WinUI control that already behaves correctly — a
  `Button`, a `ToggleButton`, a `DropDownButton` — so `InvokePattern`, `TogglePattern`, commands and
  keyboard activation are WinUI's rather than this library's. A group takes any `UIElement` at all,
  so a `NumberBox` needs no wrapper and keeps its focus.
- `Ribbon.AllowedSizes`, attached, for declaring which shapes an item accepts, and `Ribbon.GetSize`
  for reading the one it was given.
- `RibbonGroup.HasLauncher`, `LauncherFlyout` and `LauncherClick`: the small button beside a group's
  name that opens whatever the group has no room for. Off out of the box.
- `Ribbon.IsMinimized`, with a chevron in the tab strip, a double-click on a tab and `Ctrl+F1`.
  Minimised, the ribbon leaves only its tabs and gives the room back to the window; clicking a tab
  then opens it *over* the content, transiently, without changing the property — so an application
  saving `IsMinimized` records what the user asked for rather than what they last looked at.
- `RibbonStrings`, the few sentences the ribbon says on its own account: what a group's launcher is
  called, what the button a folded group becomes is called, and the two the chevron uses.
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
- `RibbonItemSize` and `RibbonItemSizes`: the three shapes an item can take, and the set of them an
  item declares it accepts.
- The layout that decides between those shapes, ahead of the control that will use it. Items step
  down through the shapes they accept before any group gives up; the group with the lowest priority
  gives way first and ties are broken from the right; a group that no longer fits folds into a
  button instead of leaving the strip; and the last resort is those buttons dropping their labels,
  one at a time and in the same order. The arrangements are generated without reference to the width
  available, which only chooses which of them to stop at, and that is what keeps the result stable,
  reversible and free of flicker.
