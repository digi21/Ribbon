# Theming Digi21.WinUI.Ribbon

The ribbon follows the light, dark and high-contrast themes with no setup at all, because every
colour it paints with is an alias of a WinUI system brush. It follows the accent colour for the same
reason.

Recolouring it means redefining a key. So does remeasuring it, and so does translating the handful of
things it says on its own account: one mechanism for everything this library lets an application
change. Retemplating is only for changing the *shape* of a control, and there is a short list at the
end of things that are not keys and cannot be moved.

## Where to put an override

In a dictionary **merged into `Application.Resources`**, which is the only place WinUI honours theme
dictionaries:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

      <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
          <ResourceDictionary x:Key="Default">
            <SolidColorBrush x:Key="RibbonTabSelectionIndicatorBrush" Color="#C50F1F" />
          </ResourceDictionary>
          <ResourceDictionary x:Key="Light">
            <SolidColorBrush x:Key="RibbonTabSelectionIndicatorBrush" Color="#B4009E" />
          </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
      </ResourceDictionary>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

The library merges its own dictionary at the bottom of that collection the first time a ribbon
control is created, so it acts as a set of defaults: anything the application declares directly, and
any dictionary it merges itself, is looked up first. There is nothing to add to `App.xaml` to make
the ribbon work — only to change it.

## One thing the application has to paint

**The page behind the ribbon.** The ribbon paints itself with a layer brush, as WinUI's own surfaces
do, and a layer brush is translucent by design: it is meant to sit on a page. Give the root of your
window a background:

```xml
<Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
```

Without it, an unpackaged WinUI window shows through black — which looks right by accident in a dark
theme and turns every word invisible in a light one.

## The brushes

| Key | Defaults to | Paints |
| --- | --- | --- |
| `RibbonBackgroundBrush` | `LayerFillColorDefaultBrush` | The ribbon, tab strip and body alike. |
| `RibbonOverlayBackgroundBrush` | `SolidBackgroundFillColorTertiaryBrush` | The ribbon when a minimised one is opened over the content. Opaque on purpose: out there it has nothing behind it. |
| `RibbonBorderBrush` | `CardStrokeColorDefaultBrush` | The rules above and below the body. |
| `RibbonTabForegroundBrush` | `TextFillColorPrimaryBrush` | The name of every tab, the one on show included. |
| `RibbonTabPointerOverBackgroundBrush` | `SubtleFillColorSecondaryBrush` | A tab under the pointer. |
| `RibbonTabPressedBackgroundBrush` | `SubtleFillColorTertiaryBrush` | A tab being pressed. |
| `RibbonTabSelectionIndicatorBrush` | `AccentFillColorDefaultBrush` | The line under the tab on show. |
| `RibbonContextualTabAccentBrush` | `AccentFillColorDefaultBrush` | The line above a contextual tab, which it wears on the strip whether or not it is the tab on show, and the colour of a heading that has none of its own. A tab in a `RibbonContextualGroup` takes that group's `Accent` instead, so this is what a contextual tab with no group is marked in. |
| `RibbonContextualHeadingForegroundBrush` | `TextFillColorPrimaryBrush` | The name on the coloured band over a set of contextual tabs. Ordinary text colour rather than the group's own: the colour identity is the band behind it, and a name drawn in an arbitrary application colour is a name that is unreadable in one theme sooner or later. |
| `RibbonGroupLabelForegroundBrush` | `TextFillColorSecondaryBrush` | The name under a group. |
| `RibbonSeparatorBrush` | `DividerStrokeColorDefaultBrush` | The rule between two columns of a group. |
| `RibbonItemPointerOverBackgroundBrush` | `SubtleFillColorSecondaryBrush` | An item under the pointer. |
| `RibbonItemPressedBackgroundBrush` | `SubtleFillColorTertiaryBrush` | An item being pressed. |
| `RibbonItemCheckedBackgroundBrush` | `AccentFillColorDefaultBrush` | A two-state item that is on. |

The name of the tab on show is deliberately the same colour as the others: the line under it is the
whole of the marking, which is what the application this library was written for does.

## The metrics

| Key | Default | Is |
| --- | --- | --- |
| `RibbonTabSelectionIndicatorHeight` | `2` | How thick the line under the tab on show is. |
| `RibbonContextualTabAccentHeight` | `2` | How thick the line above a contextual tab is. |
| `RibbonContextualTintOpacity` | `0.22` | How much of a heading's colour goes behind the band and behind the tabs of its group. The line along the top of those tabs is the same colour at full strength. |
| `RibbonTabPadding` | `12,6` | The room around a tab's name. |
| `RibbonTabCornerRadius` | `4,4,0,0` | The corners of a tab. |
| `RibbonItemCornerRadius` | `4` | The corners of an item and of a group's launcher. |
| `RibbonLauncherSize` | `16` | How big the button beside a group's name is. |

```xml
<x:Double x:Key="RibbonTabSelectionIndicatorHeight">3</x:Double>
```

Metrics go in the root of the dictionary rather than in a theme dictionary, because they are the same
in every theme.

## The sentences

The ribbon says eight things on its own account, and each of them is a key like the ones above. It
does not translate what you put in it — the name of a tab, of a group, of an item is yours and
arrives already in the user's language, because only your application knows what it is saying — so
these eight are the whole of what there is to translate.

| Key | Default | Is |
| --- | --- | --- |
| `RibbonGroupLauncherNameFormat` | `{0} options` | What the small button beside a group's name announces itself as. Takes the group's name: every launcher looks the same and does something different, so the name is the only thing that tells them apart to somebody not looking at the screen. |
| `RibbonCollapsedGroupNameFormat` | `{0}, collapsed group` | What the button a folded group becomes announces itself as. Takes the group's name. |
| `RibbonContextualTabNameFormat` | `{0}, contextual tab` | What a tab that comes and goes announces itself as. Takes the tab's name: the accent line above it says it is one only to somebody looking at the strip. |
| `RibbonContextualTabInGroupNameFormat` | `{0}, contextual tab, {1}` | The same for a tab under a coloured band. Takes the tab's name and then the heading's, because the band is what says what a set of tabs is for and it says it by being drawn. |
| `RibbonMinimizeRibbonName` | `Minimise the ribbon` | The chevron, where pressing it puts the ribbon away. |
| `RibbonExpandRibbonName` | `Expand the ribbon` | The same chevron once it has. |
| `RibbonSimplifyRibbonName` | `Simplify the ribbon` | The chevron, where pressing it drops the ribbon to one row instead. |
| `RibbonFullRibbonName` | `Show the full ribbon` | The same chevron once it has. |

```xml
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, grupo plegado</x:String>
```

Sentences go in the root of the dictionary rather than in a theme dictionary, as the metrics do: a
ribbon does not say a different thing in the dark.

Three things are worth knowing about them, and the last one is the reason this list is also available
from code.

**A placeholder at the start is written as it stands.** WPF would read the opening brace as the
beginning of a markup extension and want `{}` in front of it; WinUI reads the content of an
`x:String` literally, and the escape it does not know is a parse error that takes the application
down before its first window.

**They are read when they are said, not when the ribbon is built.** Declare one whenever you like —
in `App.xaml`, in `OnLaunched`, or into `Application.Current.Resources` an hour after the window
opened. What is already on screen picks the new wording up the next time it says anything: the
chevron when it turns over, a group when it folds, a contextual tab whenever a screen reader asks.

**A mistyped key is silent**, which is the one way a key is worse than a property somebody could have
mistyped and failed to compile. An entry nobody reads is not an error, and the sentence goes on being
said in English — which, for eight sentences of which three are only ever heard and the rest are a
tooltip, is a regression nobody notices for a long time. `Ribbon.TextResourceKeys` is the list above, in that order, so that an
application need not type the names out twice:

```csharp
foreach (string key in Ribbon.TextResourceKeys)
{
    Application.Current.Resources[key] = translations[key];
}
```

Driven that way, a key this library renames is a missing entry in your own table and an exception at
startup rather than a sentence that quietly reverts. Declaring them in XAML instead, the same
assertion is one loop at startup: every key you declared is in that list, and every key in that list
you declared.

[localisation.md](localisation.md) has all eight in nine languages.

## Motion

One thing moves that is not a colour: the change from one tab to the next. The tab arriving fades
in from the side you moved towards - choosing a tab to the right brings it in from the right - over
160 ms.

```xml
<ribbon:Ribbon TabTransition="Fade" />
```

`Fade` keeps the length and drops the movement; `None` draws nothing at all and the new tab is
simply there in the next frame. The default is `Slide`.

It is chrome over a change that has already happened. The tab is chosen, laid out and hit-testable
before the first frame of the transition is drawn, so a command clicked while one is running is
invoked; and what moves is a render transform and an opacity, neither of which the layout system can
see, so nothing is measured twice and the ribbon is exactly as tall throughout as it was before.

Two things overrule the property. A system told to show no animations - Settings, Accessibility,
Visual effects, Animation effects - is obeyed whatever the ribbon has been set to, because a user
who switched them off switched them off everywhere. And a minimised ribbon opening a tab over the
content cuts: the popup that carries the tab out there arrives with an animation of its own, and two
arrivals for one click is one too many. A tab chosen while that popup is already open is an ordinary
change of tab and is drawn like one.

How far the tab travels and how long it takes are not keys. They are one gesture, tuned once, and an
application that wants it shorter wants `Fade`, which is what shorter turns into as it approaches
zero.

## What is not a key, and why

Some numbers are not overridable, and the reason is worth knowing before you go looking for them:
**the layout decides what fits from these numbers, and the panels then place elements by the same
ones**. An application that could change one of them from outside would move one half and not the
other, and the ribbon would adopt an arrangement it had not chosen — a group predicted narrower than
it draws, quietly clipped, with nothing looking wrong.

That has happened twice in this library's own history, both times because a number the layout used
stopped matching a number the control drew with. It is not a hypothetical.

The fixed ones are three rows to a column, the height of a row, the two icon sizes, the gap between
an icon and its label, and the padding inside an item and a group.

Two of them are less fixed than they look:

- **The height of a row is a floor, not a decree.** A row is as tall as the tallest item that has to
  sit in one, so a group holding a `ComboBox` gets taller rows than one holding only buttons, and
  every group on the strip is then given the same height so that their names line up. Putting a tall
  control in a group is supported and needs nothing declared.
- **The width of an item is measured, never assumed.** It is whatever the item's own template asks
  for at each of the three shapes, so retemplating an item changes what the layout decides with.

And one number that could have been a key and is deliberately not: **how tall the coloured band over
a set of contextual tabs is**. The strip holds exactly as much room as `RibbonContextualHeading`
asks for, so the band's height is its own font and its own padding — restyle it and the strip follows.
A key would have been a second answer to that question, and the two would have come apart the first
time somebody set one without the other.

## Retemplating

Every control has a default style keyed by its type, so a `Style` targeting the type replaces it:

```xml
<Style TargetType="ribbon:RibbonButton">
  <Setter Property="Template">
    <Setter.Value>
      <ControlTemplate TargetType="ribbon:RibbonButton">
        <Border x:Name="Root" Background="{TemplateBinding Background}">
          <primitives:RibbonItemContent x:Name="PART_Content" />
        </Border>
      </ControlTemplate>
    </Setter.Value>
  </Setter>
</Style>
```

Two parts are not decoration and a template that drops them will not work:

- **`PART_Content`**, a `RibbonItemContent`, is what an item pushes its label, its icon and the shape
  the layout gave it into. It is also what answers how wide the item would be in each shape, which
  is what the layout chooses between.
- **`PART_Items`**, a `RibbonItemsPanel` inside a `PART_ItemsHost`, is the panel a group's items live
  in. It is moved whole into the flyout when the group folds, and back out again. Building the items
  from a template instead of moving that panel is precisely what this library exists not to do.
