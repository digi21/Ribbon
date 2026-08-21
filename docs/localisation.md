# Translations for Digi21.WinUI.Ribbon

Nine languages, the same nine as the sister library: lop, a point-cloud editor shipped on the
Microsoft Store, asked for them.

There is only one half here, unlike in `Digi21.WinUI.PropertyGrid`. **The ribbon does not translate
what an application puts in it**: the name of a tab, of a group, of an item is the application's, and
arrives already in the user's language, because only the application knows what it is saying. What
the ribbon says on its own account is eight sentences, and they are properties on `RibbonStrings`.

Nothing a user reads comes from a resource key, so there is no dictionary to merge for this. Set the
eight from code, once, from wherever the application keeps its translations.

**Set them early, but not too early.** In an unpackaged WinUI 3 application that means `OnLaunched`
and not the constructor of `App`.

About the placeholders: `GroupLauncherNameFormat` and `CollapsedGroupNameFormat` take the group's
name and `ContextualTabNameFormat` takes the tab's; all four must keep their `{0}`.
`ContextualTabInGroupNameFormat` takes the tab's name and then the heading's, and needs both its
`{0}` and its `{1}`. A translation that drops one produces a launcher every screen reader calls the
same thing, or a tab that never says what set it belongs to — which is the failure the sentence
exists to avoid, and it looks perfectly fine in a review. `LocalisationTests` checks that every
translation below keeps every placeholder its own English sentence uses.

Four of the eight are never seen and only heard: `GroupLauncherNameFormat`,
`CollapsedGroupNameFormat`, `ContextualTabNameFormat` and `ContextualTabInGroupNameFormat` are
automation names. They are the ones worth getting right for somebody who cannot see the icon that is
beside them — or, in the last two cases, cannot see that the strip has a name on it that was not
there a moment ago, nor the coloured band that says which set of tabs it arrived with. Each language
below uses the words Office uses in it for one: *pestaña contextual*, *onglet contextuel*,
*kontextbezogene Registerkarte*.

Four of them are what the chevron says, two per behaviour: the ribbon can be set to simplify when it
is collapsed or to minimise, and a button announcing that it minimises a ribbon it is about to
simplify is worse than one that says nothing. An application that has fixed
`CollapseBehavior` still translates all four, because it costs one line and the wrong one is worse
than a missing one.

The eight are read by `LocalisationTests`, which also checks that this file mentions every property of
the class and that every language below sets all of them, so the file cannot quietly drift away from
the code.

## Catalan (`ca`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opcions de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grup plegat";
RibbonStrings.ContextualTabNameFormat = "{0}, pestanya contextual";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, pestanya contextual, {1}";
RibbonStrings.MinimizeRibbonName = "Minimitza la cinta d'opcions";
RibbonStrings.ExpandRibbonName = "Expandeix la cinta d'opcions";
RibbonStrings.SimplifyRibbonName = "Simplifica la cinta d'opcions";
RibbonStrings.FullRibbonName = "Mostra la cinta d'opcions completa";
```

## English (`en`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "{0} options";
RibbonStrings.CollapsedGroupNameFormat = "{0}, collapsed group";
RibbonStrings.ContextualTabNameFormat = "{0}, contextual tab";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, contextual tab, {1}";
RibbonStrings.MinimizeRibbonName = "Minimise the ribbon";
RibbonStrings.ExpandRibbonName = "Expand the ribbon";
RibbonStrings.SimplifyRibbonName = "Simplify the ribbon";
RibbonStrings.FullRibbonName = "Show the full ribbon";
```

## Basque (`eu`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "{0} aukerak";
RibbonStrings.CollapsedGroupNameFormat = "{0}, tolestutako taldea";
RibbonStrings.ContextualTabNameFormat = "{0}, testuinguruko fitxa";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, testuinguruko fitxa, {1}";
RibbonStrings.MinimizeRibbonName = "Zinta minimizatu";
RibbonStrings.ExpandRibbonName = "Zinta zabaldu";
RibbonStrings.SimplifyRibbonName = "Zinta sinplifikatu";
RibbonStrings.FullRibbonName = "Zinta osoa erakutsi";
```

## French (`fr`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Options de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, groupe réduit";
RibbonStrings.ContextualTabNameFormat = "{0}, onglet contextuel";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, onglet contextuel, {1}";
RibbonStrings.MinimizeRibbonName = "Réduire le ruban";
RibbonStrings.ExpandRibbonName = "Développer le ruban";
RibbonStrings.SimplifyRibbonName = "Simplifier le ruban";
RibbonStrings.FullRibbonName = "Afficher le ruban complet";
```

## Galician (`gl`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opcións de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo pregado";
RibbonStrings.ContextualTabNameFormat = "{0}, separador contextual";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, separador contextual, {1}";
RibbonStrings.MinimizeRibbonName = "Minimizar a cinta de opcións";
RibbonStrings.ExpandRibbonName = "Expandir a cinta de opcións";
RibbonStrings.SimplifyRibbonName = "Simplificar a cinta de opcións";
RibbonStrings.FullRibbonName = "Mostrar a cinta de opcións completa";
```

## German (`de`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Optionen für {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, reduzierte Gruppe";
RibbonStrings.ContextualTabNameFormat = "{0}, kontextbezogene Registerkarte";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, kontextbezogene Registerkarte, {1}";
RibbonStrings.MinimizeRibbonName = "Menüband minimieren";
RibbonStrings.ExpandRibbonName = "Menüband erweitern";
RibbonStrings.SimplifyRibbonName = "Menüband vereinfachen";
RibbonStrings.FullRibbonName = "Vollständiges Menüband anzeigen";
```

## Italian (`it`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opzioni di {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, gruppo compresso";
RibbonStrings.ContextualTabNameFormat = "{0}, scheda contestuale";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, scheda contestuale, {1}";
RibbonStrings.MinimizeRibbonName = "Riduci la barra multifunzione";
RibbonStrings.ExpandRibbonName = "Espandi la barra multifunzione";
RibbonStrings.SimplifyRibbonName = "Semplifica la barra multifunzione";
RibbonStrings.FullRibbonName = "Mostra la barra multifunzione completa";
```

## Portuguese (`pt`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opções de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo recolhido";
RibbonStrings.ContextualTabNameFormat = "{0}, separador contextual";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, separador contextual, {1}";
RibbonStrings.MinimizeRibbonName = "Minimizar o friso";
RibbonStrings.ExpandRibbonName = "Expandir o friso";
RibbonStrings.SimplifyRibbonName = "Simplificar o friso";
RibbonStrings.FullRibbonName = "Mostrar o friso completo";
```

## Spanish (`es`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opciones de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo plegado";
RibbonStrings.ContextualTabNameFormat = "{0}, pestaña contextual";
RibbonStrings.ContextualTabInGroupNameFormat = "{0}, pestaña contextual, {1}";
RibbonStrings.MinimizeRibbonName = "Minimizar la cinta de opciones";
RibbonStrings.ExpandRibbonName = "Expandir la cinta de opciones";
RibbonStrings.SimplifyRibbonName = "Simplificar la cinta de opciones";
RibbonStrings.FullRibbonName = "Mostrar la cinta de opciones completa";
```

## The words for "ribbon"

Each language uses the word Office uses in it, rather than a literal translation of the English,
because that is the word a user of that language has been reading for twenty years: *cinta de
opciones* in Spanish, *cinta d'opcions* in Catalan, *cinta de opcións* in Galician, *ruban* in
French, *Menüband* in German, *barra multifunzione* in Italian, *friso* in European Portuguese and
*zinta* in Basque.

Brazilian Portuguese says *faixa de opções* rather than *friso*. If your application ships in
`pt-BR`, use that.
