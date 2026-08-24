# Translations for Digi21.WinUI.Ribbon

Nine languages, the same nine as the sister libraries.

There is only one half here, unlike in `Digi21.WinUI.PropertyGrid`. **The ribbon does not translate
what an application puts in it**: the name of a tab, of a group, of an item is the application's, and
arrives already in the user's language, because only the application knows what it is saying. What
the ribbon says on its own account is eight sentences, and each of them is a resource key.

## Where they go

Where the brushes and the metrics go: a dictionary merged into `Application.Resources`, or the root
of that dictionary itself. [theming.md](theming.md) has the shape of that file, and these keys sit in
it beside the colours — one mechanism for everything this library lets an application change.

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    </ResourceDictionary.MergedDictionaries>

    <x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, grupo plegado</x:String>
  </ResourceDictionary>
</Application.Resources>
```

From code it is the same eight keys, written wherever the application keeps its translations:

```csharp
Application.Current.Resources["RibbonCollapsedGroupNameFormat"] = "{0}, grupo plegado";
```

**Whenever is early enough.** The library merges its own English underneath the first time a ribbon
control is created, and what an application declares wins over that whether it was declared before
then or long afterwards: each sentence is read at the moment it is said rather than held from the
moment a control was built. A ribbon already on screen announces the new wording the next time it
announces anything.

**A placeholder at the start is written as it stands.** WPF reads an opening brace in a value as the
beginning of a markup extension and wants `{}` in front of one; WinUI reads the content of an
`x:String` literally, and the escape it does not know is a parse error that takes the application
down before its first window. There is more of that kind in [winui.md](winui.md).

## The eight keys

| Key | English | Takes |
| --- | --- | --- |
| `RibbonGroupLauncherNameFormat` | `{0} options` | The group's name. |
| `RibbonCollapsedGroupNameFormat` | `{0}, collapsed group` | The group's name. |
| `RibbonContextualTabNameFormat` | `{0}, contextual tab` | The tab's name. |
| `RibbonContextualTabInGroupNameFormat` | `{0}, contextual tab, {1}` | The tab's name, then the heading's. |
| `RibbonMinimizeRibbonName` | `Minimise the ribbon` | — |
| `RibbonExpandRibbonName` | `Expand the ribbon` | — |
| `RibbonSimplifyRibbonName` | `Simplify the ribbon` | — |
| `RibbonFullRibbonName` | `Show the full ribbon` | — |

**A mistyped key is silent**, which is the one way a key is worse than a property: an entry nobody
reads is not an error, and the sentence goes on being said in English. `Ribbon.TextResourceKeys` is
the list above, in that order, so that an application need not type the names out twice:

```csharp
foreach (string key in Ribbon.TextResourceKeys)
{
    Application.Current.Resources[key] = translations[key];
}
```

Driven that way, a key this library renames is a missing entry in the application's own table and an
exception at startup, rather than a sentence that quietly reverts to English and is noticed years
later by somebody who cannot see the screen. An application that declares them in XAML instead can
assert the same thing: every key it declared is in that list, and every key in that list it declared.

About the placeholders: `RibbonGroupLauncherNameFormat`, `RibbonCollapsedGroupNameFormat` and
`RibbonContextualTabNameFormat` take one name and must keep their `{0}`.
`RibbonContextualTabInGroupNameFormat` takes the tab's name and then the heading's, and needs both
its `{0}` and its `{1}`. A translation that drops one produces a launcher every screen reader calls
the same thing, or a tab that never says what set it belongs to — which is the failure the sentence
exists to avoid, and it looks perfectly fine in a review. `LocalisationTests` checks that every
translation below keeps every placeholder its own English sentence uses.

Three of the eight are only ever heard: `RibbonCollapsedGroupNameFormat`,
`RibbonContextualTabNameFormat` and `RibbonContextualTabInGroupNameFormat` are automation names and
nothing else. They are the ones worth getting right for somebody who cannot see the icon beside them,
or, in the last two cases, cannot see that the strip has a name on it that was not there a moment
ago, nor the coloured band that says which set of tabs it arrived with. Each language below uses the
words Office uses in it for one: *pestaña contextual*, *onglet contextuel*, *kontextbezogene
Registerkarte*.

The other five are a tooltip as well as an automation name, so a wrong one there is at least visible.
Four of them are what the chevron says, two per behaviour: the ribbon can be set to simplify when it
is collapsed or to minimise, and a button announcing that it minimises a ribbon it is about to
simplify is worse than one that says nothing. An application that has fixed `CollapseBehavior` still
translates all four, because it costs one line and the wrong one is worse than a missing one. The
fifth is what the small button beside a group's name says, which is a different sentence for every
group on the strip.

The eight are read by `LocalisationTests`, which checks that this file mentions every key the library
reads and that every language below sets all of them, and by `RibbonTextTests`, which checks that the
library's own dictionary declares the same eight with the same English. Neither file can quietly
drift away from the code.

## Catalan (`ca`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Opcions de {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, grup plegat</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, pestanya contextual</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, pestanya contextual, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Minimitza la cinta d'opcions</x:String>
<x:String x:Key="RibbonExpandRibbonName">Expandeix la cinta d'opcions</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Simplifica la cinta d'opcions</x:String>
<x:String x:Key="RibbonFullRibbonName">Mostra la cinta d'opcions completa</x:String>
```

## English (`en`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">{0} options</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, collapsed group</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, contextual tab</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, contextual tab, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Minimise the ribbon</x:String>
<x:String x:Key="RibbonExpandRibbonName">Expand the ribbon</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Simplify the ribbon</x:String>
<x:String x:Key="RibbonFullRibbonName">Show the full ribbon</x:String>
```

## Basque (`eu`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">{0} aukerak</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, tolestutako taldea</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, testuinguruko fitxa</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, testuinguruko fitxa, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Zinta minimizatu</x:String>
<x:String x:Key="RibbonExpandRibbonName">Zinta zabaldu</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Zinta sinplifikatu</x:String>
<x:String x:Key="RibbonFullRibbonName">Zinta osoa erakutsi</x:String>
```

## French (`fr`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Options de {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, groupe réduit</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, onglet contextuel</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, onglet contextuel, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Réduire le ruban</x:String>
<x:String x:Key="RibbonExpandRibbonName">Développer le ruban</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Simplifier le ruban</x:String>
<x:String x:Key="RibbonFullRibbonName">Afficher le ruban complet</x:String>
```

## Galician (`gl`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Opcións de {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, grupo pregado</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, separador contextual</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, separador contextual, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Minimizar a cinta de opcións</x:String>
<x:String x:Key="RibbonExpandRibbonName">Expandir a cinta de opcións</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Simplificar a cinta de opcións</x:String>
<x:String x:Key="RibbonFullRibbonName">Mostrar a cinta de opcións completa</x:String>
```

## German (`de`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Optionen für {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, reduzierte Gruppe</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, kontextbezogene Registerkarte</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, kontextbezogene Registerkarte, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Menüband minimieren</x:String>
<x:String x:Key="RibbonExpandRibbonName">Menüband erweitern</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Menüband vereinfachen</x:String>
<x:String x:Key="RibbonFullRibbonName">Vollständiges Menüband anzeigen</x:String>
```

## Italian (`it`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Opzioni di {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, gruppo compresso</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, scheda contestuale</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, scheda contestuale, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Riduci la barra multifunzione</x:String>
<x:String x:Key="RibbonExpandRibbonName">Espandi la barra multifunzione</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Semplifica la barra multifunzione</x:String>
<x:String x:Key="RibbonFullRibbonName">Mostra la barra multifunzione completa</x:String>
```

## Portuguese (`pt`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Opções de {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, grupo recolhido</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, separador contextual</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, separador contextual, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Minimizar o friso</x:String>
<x:String x:Key="RibbonExpandRibbonName">Expandir o friso</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Simplificar o friso</x:String>
<x:String x:Key="RibbonFullRibbonName">Mostrar o friso completo</x:String>
```

## Spanish (`es`)

```xml
<x:String x:Key="RibbonGroupLauncherNameFormat">Opciones de {0}</x:String>
<x:String x:Key="RibbonCollapsedGroupNameFormat">{0}, grupo plegado</x:String>
<x:String x:Key="RibbonContextualTabNameFormat">{0}, pestaña contextual</x:String>
<x:String x:Key="RibbonContextualTabInGroupNameFormat">{0}, pestaña contextual, {1}</x:String>
<x:String x:Key="RibbonMinimizeRibbonName">Minimizar la cinta de opciones</x:String>
<x:String x:Key="RibbonExpandRibbonName">Expandir la cinta de opciones</x:String>
<x:String x:Key="RibbonSimplifyRibbonName">Simplificar la cinta de opciones</x:String>
<x:String x:Key="RibbonFullRibbonName">Mostrar la cinta de opciones completa</x:String>
```

## The words for "ribbon"

Each language uses the word Office uses in it, rather than a literal translation of the English,
because that is the word a user of that language has been reading for twenty years: *cinta de
opciones* in Spanish, *cinta d'opcions* in Catalan, *cinta de opcións* in Galician, *ruban* in
French, *Menüband* in German, *barra multifunzione* in Italian, *friso* in European Portuguese and
*zinta* in Basque.

Brazilian Portuguese says *faixa de opções* rather than *friso*. If your application ships in
`pt-BR`, use that.
