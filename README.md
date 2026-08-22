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
  <img src="https://raw.githubusercontent.com/digi21/Ribbon/main/assets/gallery.png" width="820" alt="The gallery: a Home tab with Clipboard, Font and Paragraph groups, items at three sizes, a hosted number box and combo box, the chevron that puts the ribbon away, and a Picture Tools heading in its own colour over two contextual tabs that have just arrived" />
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
- **Contextual tabs**: a tab that is on the strip only while it is worth having. Declare it once with
  its groups, set `IsContextual`, and tie `IsActive` to whatever the tab is about — a selection
  waiting to be dealt with, a table the caret is in. It arrives marked with an accent line and steps
  forward as it comes, unless you set `SelectsWhenActivated="False"`; when it goes, the ribbon goes
  back to the tab the user came from. It never changes the ribbon's height, and nothing is rebuilt on
  the way, so a tab that comes and goes twenty times a minute costs one build.
- **A coloured heading over a set of them**, which is Office's Table Tools and Picture Tools.
  `RibbonContextualGroup` carries a name and a colour; point any number of contextual tabs at one and
  they are drawn under one band, in one colour, with that colour tinting the tabs themselves - which
  is what makes a contextual tab tell itself apart from a fixed one at a glance rather than only in
  the second it arrives. It costs the strip no height at all: the room for the band is held from the
  moment a tab is given a group, so a tab arriving fills room that was already there. Both are in
  [docs/contextual-tabs.md](https://github.com/digi21/Ribbon/blob/main/docs/contextual-tabs.md),
  including what happens when a tab disappears out from under you.
- **UI Automation that works**: `InvokePattern` on buttons, `TogglePattern` on two-state ones,
  `TabItem` with `SelectionItemPattern` on every tab and `SelectionPattern` on the ribbon itself, and
  `AutomationProperties.Name` on everything, so the application on top can be driven by a test rather
  than by screen coordinates. A tab that is off the strip is out of the automation tree rather than
  present and unpressable, and a tab arriving says so — as a CLR event in process, and as a
  `StructureChanged` event out of it.
- **Light and dark**, following the system, built on the WinUI theme resources, with monochrome icons
  tinted from the foreground so they stay visible in both.
- **One row, as in Office**: `DisplayMode="Simplified"` lays the whole strip out in a single row,
  with the group names off and every item beside its label or down to its icon. What does not fit
  folds into the group's button exactly as it does in a full ribbon squeezed hard - there is no
  second overflow mechanism to learn - and a group holding something that cannot be drawn in one
  row is its button at every width, with everything it holds laid out in the flyout the way a full
  ribbon would. It is independent of minimizing, and the controls in a group are the same objects
  before and after the switch.
- **A change of tab you can see**: the tab arriving fades in from the side you moved towards, in
  160 ms. It is a render transform and an opacity, so the layout neither sees it nor runs again for
  it, and the tab is chosen, laid out and clickable before the first frame of it is drawn.
  `TabTransition="Fade"` takes the movement off and `"None"` takes the whole thing off. Whatever it
  says, a system told to show no animations is obeyed, and a minimised ribbon opening a tab over the
  content cuts: the popup that carries it already arrives with an animation of its own.
- **Keyboard**: the whole ribbon is one stop on the way through the window. `Tab` reaches it at the
  tab on show, whichever way round it was going, and the next `Tab` leaves it - a ribbon that made
  every command a tab stop would be one nobody tabs past twice. Everything inside is reached with the
  arrow keys, as it is in every WinUI control that holds a strip of anything: left and right along
  the tabs, changing the ribbon as they go, `Home` and `End` to either end of the strip, down into
  the commands and then the arrows between them - across groups, into the button a folded group
  became, and up out of the top row back to the tab names. `Esc` comes back out: to the strip from a
  command, which is also the way out of a hosted `NumberBox` that has taken the arrow keys for its
  caret, and out of the ribbon from the strip, back to whatever the focus came in from.
  [docs/keyboard.md](https://github.com/digi21/Ribbon/blob/main/docs/keyboard.md) is the table.
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

## A ribbon in one screenful

```xml
<Page
    xmlns:ribbon="using:Digi21.WinUI.Ribbon"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <ribbon:Ribbon>
            <ribbon:RibbonTab Label="Home">
                <ribbon:RibbonGroup Label="Clipboard" Priority="0">
                    <ribbon:RibbonGroup.IconSource>
                        <SymbolIconSource Symbol="Paste" />
                    </ribbon:RibbonGroup.IconSource>

                    <!-- Takes every shape: icon above text, icon beside text, icon alone. -->
                    <ribbon:RibbonButton Label="Paste" Click="OnPaste">
                        <ribbon:RibbonButton.IconSource>
                            <SymbolIconSource Symbol="Paste" />
                        </ribbon:RibbonButton.IconSource>
                    </ribbon:RibbonButton>

                    <!-- Never grows past its icon and its label. -->
                    <ribbon:RibbonButton Label="Cut" ribbon:Ribbon.AllowedSizes="Normal,Small">
                        <ribbon:RibbonButton.IconSource>
                            <SymbolIconSource Symbol="Cut" />
                        </ribbon:RibbonButton.IconSource>
                    </ribbon:RibbonButton>
                </ribbon:RibbonGroup>

                <ribbon:RibbonGroup Label="Font" Priority="10">
                    <ribbon:RibbonGroup.IconSource>
                        <SymbolIconSource Symbol="Font" />
                    </ribbon:RibbonGroup.IconSource>

                    <ribbon:RibbonToggleButton Label="Bold" ribbon:Ribbon.AllowedSizes="Small">
                        <ribbon:RibbonToggleButton.IconSource>
                            <SymbolIconSource Symbol="Bold" />
                        </ribbon:RibbonToggleButton.IconSource>
                    </ribbon:RibbonToggleButton>

                    <!-- Any WinUI control, with a name beside it, keeping its own focus. -->
                    <ribbon:RibbonContentItem Label="Size">
                        <NumberBox x:Name="FontSize" Width="92" Value="12" />
                    </ribbon:RibbonContentItem>
                </ribbon:RibbonGroup>
            </ribbon:RibbonTab>
        </ribbon:Ribbon>

        <ScrollViewer Grid.Row="1">
            <!-- The application's own content. -->
        </ScrollViewer>
    </Grid>
</Page>
```

Three things in there are worth pointing at. `Priority` is the order the groups give way in as the
window narrows — the lowest goes first. `Ribbon.AllowedSizes` is how an item says which shapes it
accepts; leave it off and the item takes all three. And the row the ribbon sits in is `Auto`: the
ribbon is a strip with the application's content under it, and it asks for the height it needs.

**Give the root a background.** The ribbon paints itself with a WinUI layer brush, which is
translucent by design because it is meant to sit on a page. Without
`{ThemeResource ApplicationPageBackgroundThemeBrush}` behind it, an unpackaged WinUI window shows
through black and every word in a light theme goes invisible.

### The same from code

The programmatic API is not an afterthought: the first application to use this library generates its
ribbon from its own command registry, so everything the markup does is a property or a collection.

```csharp
var ribbon = new Ribbon();
var home = new RibbonTab { Label = "Home" };
var clipboard = new RibbonGroup { Label = "Clipboard", Priority = 0 };

var paste = new RibbonButton
{
    Label = "Paste",
    IconSource = new SymbolIconSource { Symbol = Symbol.Paste },
};

paste.Click += OnPaste;
Ribbon.SetAllowedSizes(paste, RibbonItemSizes.All);

clipboard.Items.Add(paste);
home.Groups.Add(clipboard);
ribbon.Tabs.Add(home);
```

Tabs, groups and items can be added and removed while the ribbon is on screen, and a control an
application put in a group stays the same object through every relayout — that is the promise the
whole library is built on, so keep the reference in a field and use it.

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

What the ribbon says on its own account is seven sentences, and three of them are never seen — they
are what a screen reader is told about a group's launcher, about the button a folded group becomes,
and about a contextual tab. They are properties on `RibbonStrings`, set once from wherever you keep
your translations:

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opciones de {0}";
```

[docs/localisation.md](https://github.com/digi21/Ribbon/blob/main/docs/localisation.md) has all seven
in Catalan, English, Basque, French, Galician, German, Italian, Portuguese and Spanish, each using
the word Office uses in that language rather than a translation of the English one.

## Documentation

- [How the ribbon decides what fits](https://github.com/digi21/Ribbon/blob/main/docs/layout.md) —
  the three shapes, the columns of three, the order groups give way in, and why a width can only ever
  have one answer.
- [Contextual tabs](https://github.com/digi21/Ribbon/blob/main/docs/contextual-tabs.md) — a tab that
  comes and goes: the three properties, where it appears, where the ribbon goes when it leaves, and
  what it does to the height, to a simplified ribbon and to a minimised one.
- [The keyboard](https://github.com/digi21/Ribbon/blob/main/docs/keyboard.md) — one stop for the
  whole ribbon and the arrow keys inside it: every key in a table, why `Tab` does not walk the
  commands, and how to get out of a control that has taken the arrows for itself.
- [Theming](https://github.com/digi21/Ribbon/blob/main/docs/theming.md) — every brush and metric key,
  and what is deliberately not one.
- [Translations](https://github.com/digi21/Ribbon/blob/main/docs/localisation.md) — the four
  sentences the ribbon says on its own account, in nine languages.
- [What WinUI cost this library](https://github.com/digi21/Ribbon/blob/main/docs/winui.md) — the
  things that are not guessable, fail quietly, and each cost an afternoon. Worth reading before
  concluding that something is impossible.

## Status

**0.1.0 is the first published version.** It is in use: the application it was written for ships its
whole ribbon on it, which is what every behaviour described above was measured against.

It is a `0.x` on purpose. Everything here is settled enough to build on and nothing is frozen: a name
or a default may still move before `1.0`, and when one does it is in
[CHANGELOG.md](https://github.com/digi21/Ribbon/blob/main/CHANGELOG.md) with the reason. Between
releases the repository builds as `0.1.0-dev.N`, where `N` is the number of commits since the tag, so
a local feed can hold several of them side by side.

What follows is what this version does not do.

## Not in this version

The architecture is meant not to rule them out, but none of these is being built now:

- Keytips.
- A backstage or File menu.
- A fixed palette of heading colours to choose from, as Office offers. A `RibbonContextualGroup`
  takes a brush of the application's own instead, which is less API and one less list to keep in step
  with a theme.
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
