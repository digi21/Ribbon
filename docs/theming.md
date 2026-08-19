# Theming Digi21.WinUI.Ribbon

The ribbon follows the light, dark and high-contrast themes with no setup at all, because every
colour it paints with is an alias of a WinUI system brush. It follows the accent colour for the same
reason.

Recolouring it means redefining a key. Retemplating is only for changing the *shape* of a control,
and there is a short list at the end of things that are not keys and cannot be moved.

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
| `RibbonTabPadding` | `12,6` | The room around a tab's name. |
| `RibbonTabCornerRadius` | `4,4,0,0` | The corners of a tab. |
| `RibbonItemCornerRadius` | `4` | The corners of an item and of a group's launcher. |
| `RibbonLauncherSize` | `16` | How big the button beside a group's name is. |

```xml
<x:Double x:Key="RibbonTabSelectionIndicatorHeight">3</x:Double>
```

Metrics go in the root of the dictionary rather than in a theme dictionary, because they are the same
in every theme.

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
