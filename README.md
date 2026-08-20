<img src="https://raw.githubusercontent.com/digi21/Ribbon/main/assets/icon-256.png" width="96" alt="" />

# Digi21.WinUI.Ribbon

[![CI](https://github.com/digi21/Ribbon/actions/workflows/ci.yml/badge.svg)](https://github.com/digi21/Ribbon/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

An Office-style ribbon for WinUI 3. Tabs hold groups, groups hold items, and the whole thing gives
way gracefully as the window narrows: items step down through the size variants they declare, and a
group that no longer fits collapses into a button with a drop-down that holds it whole. Nothing ever
scrolls out of reach, and nothing is ever built twice.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/digi21/Ribbon/main/assets/gallery-dark.png" />
  <img src="https://raw.githubusercontent.com/digi21/Ribbon/main/assets/gallery.png" width="820" alt="The gallery: a Home tab with Clipboard, Font and Paragraph groups, items at three sizes, a hosted number box and combo box, and the chevron that puts the ribbon away" />
</picture>

## Why this exists

The first application to use it had a ribbon already, and replaced it for three reasons. They are
what the library is measured against:

- **One live instance per item.** Ribbons that reflow by cloning item templates leave several live
  controls for the same setting, so everything the application touches from code has to be kept in a
  list and written to every copy. Here, whoever puts a control in a group keeps the reference in a
  field and trusts it — through overflow, through relayout, through a change of tab.
- **Groups collapse, they do not vanish.** When the width runs out, a group turns into a button with
  its icon and its label that opens the whole group in a flyout, as in Office — not a pair of arrows
  that scrolls it off the screen.
- **Hosted controls keep the focus.** A `NumberBox` in a group can be typed into with no per-control
  opt-in, because the ribbon does not take the focus away from the content it hosts.

## Features

- **Tabs and groups**, declared in XAML or built at run time — the programmatic API is first class,
  because an application that generates its ribbon from its own command registry needs it to be.
- **Size variants per item**: `Small` (icon only), `Normal` (icon and text side by side) and `Large`
  (icon above, text below). Each item declares which ones it accepts.
- **Office-style degradation**: items step down through their variants first; then the group with
  the lowest priority collapses into a drop-down. Reversible, stable, and never flickering between
  two arrangements at the width where one turns into the other.
- **Any WinUI control in a group** — `NumberBox`, `ComboBox`, `ToggleSwitch`, a colour picker —
  without losing the focus and without the consumer setting anything. Wrap it in a
  `RibbonContentItem` to put a name beside it, and the control takes that name for a screen reader
  too; drop it in bare and give it an `AutomationProperties.Name` of your own, because there is then
  no label to borrow one from.
- **A launcher button per group**, off by default, that opens a flyout when switched on.
- **One gesture for asking for less of it**: the chevron in the corner, a double-click on a tab, or
  `Ctrl+F1`. `CollapseBehavior` says what it means, and out of the box it means one row, not none:
  the commands are still there afterwards, because a chevron in a corner is easy to press by
  accident and pressing one should not leave somebody in front of a window with nothing to press.
  Set it to `Minimize` for the Office behaviour - only the tab strip is left, clicking a tab opens
  the ribbon *over* the content rather than pushing it down, and that overlay closes on a command,
  on a click outside and on `Esc` without minimizing or restoring anything - or to `None` to take
  the chevron off altogether. `DisplayMode` and `IsMinimized` are ordinary two-way properties either
  way, so an application can offer the state the gesture does not reach and save what the user chose
  with the rest of its settings.
- **UI Automation that works**: `InvokePattern` on buttons, `TogglePattern` on two-state ones, and
  `AutomationProperties.Name` on everything, so the application on top can be driven by a test rather
  than by screen coordinates.
- **Light and dark**, following the system, built on the WinUI theme resources, with monochrome icons
  tinted from the foreground so they stay visible in both.
- **One row, as in Office**: `DisplayMode="Simplified"` lays the whole strip out in a single row,
  with the group names off and every item beside its label or down to its icon. What does not fit
  folds into the group's button exactly as it does in a full ribbon squeezed hard - there is no
  second overflow mechanism to learn - and a group holding something that cannot be drawn in one
  row is its button at every width, with everything it holds laid out in the flyout the way a full
  ribbon would. It is independent of minimizing, and the controls in a group are the same objects
  before and after the switch.
- **Keyboard**: Tab and the arrow keys move between tabs and items, Esc closes a drop-down.
- **Correct at 100 %, 125 %, 150 % and 200 %.**
- **The consumer supplies the text, already translated.** The library does not translate ribbon
  content; only its own internal strings, which ship in nine languages.

## Requirements

- Windows App SDK 1.8 or later.
- .NET 8 or later.
- Windows 10 version 1809 (build 17763) or later.

## Installation

```
dotnet add package Digi21.WinUI.Ribbon
```

### Theming

Every colour the ribbon paints with is an alias of a WinUI system brush, so it follows the accent
colour, both themes and high contrast on its own. Redeclare a key to change one:

```xml
<SolidColorBrush x:Key="RibbonTabSelectionIndicatorBrush" Color="#C50F1F" />
```

One thing is yours rather than the library's: **the page behind it**. The ribbon paints with a layer
brush, as WinUI's own surfaces do, and a layer brush is translucent by design — give the root of your
window a `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"`, or an unpackaged WinUI
window will show through black and turn every word invisible in a light theme.

[docs/theming.md](https://github.com/digi21/Ribbon/blob/main/docs/theming.md) has the full list of
keys, where an override has to go, how to retemplate a control, and what is deliberately not a key.

### Other languages

The ribbon does not translate what you put in it: the name of a tab, of a group, of an item is yours
and arrives already in the user's language, because only your application knows what it is saying.

What the ribbon says on its own account is four sentences, and two of them are never seen — they are
what a screen reader is told about a group's launcher and about the button a folded group becomes.
They are properties on `RibbonStrings`, set once from wherever you keep your translations:

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opciones de {0}";
```

[docs/localisation.md](https://github.com/digi21/Ribbon/blob/main/docs/localisation.md) has all four
in Catalan, English, Basque, French, Galician, German, Italian, Portuguese and Spanish, each using
the word Office uses in that language rather than a translation of the English one.

## Documentation

- [How the ribbon decides what fits](https://github.com/digi21/Ribbon/blob/main/docs/layout.md) —
  the three shapes, the columns of three, the order groups give way in, and why a width can only ever
  have one answer.
- [Theming](https://github.com/digi21/Ribbon/blob/main/docs/theming.md) — every brush and metric key,
  and what is deliberately not one.
- [Translations](https://github.com/digi21/Ribbon/blob/main/docs/localisation.md) — the four
  sentences the ribbon says on its own account, in nine languages.
- [What WinUI cost this library](https://github.com/digi21/Ribbon/blob/main/docs/winui.md) — the
  things that are not guessable, fail quietly, and each cost an afternoon. Worth reading before
  concluding that something is impossible.

## Status

The API is being designed and nothing has been released. The version this repository builds is
`0.1.0-dev.N`, where `N` is the number of commits, so every build is a distinct pre-release that a
local feed can hold alongside the last one.

The quickstart, the programmatic API, theming and the guides in `docs/` land here as the control
takes shape. What follows is settled: it is the shape of the first version, and what it will not do.

## Not in this version

The architecture is meant not to rule them out, but none of these is being built now:

- Keytips.
- A backstage or File menu.
- Contextual tabs.
- User customization of the ribbon.

## Sample

`samples/RibbonGallery` shows what the control can do, including a window you can narrow and widen
to watch the layout degrade in real time:

```
dotnet run --project samples/RibbonGallery
```

It is a demonstration, not a test bench: named bug reproductions and the measurements that catch
regressions live in a separate, private harness, so that what a prospective user runs is only the
library showing itself off.

## Contributing

Issues and pull requests are welcome — see
[CONTRIBUTING.md](https://github.com/digi21/Ribbon/blob/main/CONTRIBUTING.md). What changes between
versions is recorded in
[CHANGELOG.md](https://github.com/digi21/Ribbon/blob/main/CHANGELOG.md).

## License

[MIT](https://github.com/digi21/Ribbon/blob/main/LICENSE)
