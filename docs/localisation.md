# Translations for Digi21.WinUI.Ribbon

Nine languages, the same nine as the sister library: lop, a point-cloud editor shipped on the
Microsoft Store, asked for them.

There is only one half here, unlike in `Digi21.WinUI.PropertyGrid`. **The ribbon does not translate
what an application puts in it**: the name of a tab, of a group, of an item is the application's, and
arrives already in the user's language, because only the application knows what it is saying. What
the ribbon says on its own account is four sentences, and they are properties on `RibbonStrings`.

Nothing a user reads comes from a resource key, so there is no dictionary to merge for this. Set the
four from code, once, from wherever the application keeps its translations.

**Set them early, but not too early.** In an unpackaged WinUI 3 application that means `OnLaunched`
and not the constructor of `App`.

About the placeholders: `GroupLauncherNameFormat` and `CollapsedGroupNameFormat` each take the
group's name and must keep their `{0}`. A translation that drops it produces a launcher every screen
reader calls the same thing, which is the failure the sentence exists to avoid. `LocalisationTests`
checks that every translation below still has it.

Two of the four are never seen and only heard: `GroupLauncherNameFormat` and
`CollapsedGroupNameFormat` are automation names. They are the ones worth getting right for somebody
who cannot see the icon that is beside them.

The four are read by `LocalisationTests`, which also checks that this file mentions every property of
the class and that every language below sets all of them, so the file cannot quietly drift away from
the code.

## Catalan (`ca`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opcions de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grup plegat";
RibbonStrings.MinimizeRibbonName = "Minimitza la cinta d'opcions";
RibbonStrings.ExpandRibbonName = "Expandeix la cinta d'opcions";
```

## English (`en`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "{0} options";
RibbonStrings.CollapsedGroupNameFormat = "{0}, collapsed group";
RibbonStrings.MinimizeRibbonName = "Minimise the ribbon";
RibbonStrings.ExpandRibbonName = "Expand the ribbon";
```

## Basque (`eu`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "{0} aukerak";
RibbonStrings.CollapsedGroupNameFormat = "{0}, tolestutako taldea";
RibbonStrings.MinimizeRibbonName = "Zinta minimizatu";
RibbonStrings.ExpandRibbonName = "Zinta zabaldu";
```

## French (`fr`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Options de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, groupe réduit";
RibbonStrings.MinimizeRibbonName = "Réduire le ruban";
RibbonStrings.ExpandRibbonName = "Développer le ruban";
```

## Galician (`gl`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opcións de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo pregado";
RibbonStrings.MinimizeRibbonName = "Minimizar a cinta de opcións";
RibbonStrings.ExpandRibbonName = "Expandir a cinta de opcións";
```

## German (`de`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Optionen für {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, reduzierte Gruppe";
RibbonStrings.MinimizeRibbonName = "Menüband minimieren";
RibbonStrings.ExpandRibbonName = "Menüband erweitern";
```

## Italian (`it`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opzioni di {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, gruppo compresso";
RibbonStrings.MinimizeRibbonName = "Riduci la barra multifunzione";
RibbonStrings.ExpandRibbonName = "Espandi la barra multifunzione";
```

## Portuguese (`pt`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opções de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo recolhido";
RibbonStrings.MinimizeRibbonName = "Minimizar o friso";
RibbonStrings.ExpandRibbonName = "Expandir o friso";
```

## Spanish (`es`)

```csharp
RibbonStrings.GroupLauncherNameFormat = "Opciones de {0}";
RibbonStrings.CollapsedGroupNameFormat = "{0}, grupo plegado";
RibbonStrings.MinimizeRibbonName = "Minimizar la cinta de opciones";
RibbonStrings.ExpandRibbonName = "Expandir la cinta de opciones";
```

## The words for "ribbon"

Each language uses the word Office uses in it, rather than a literal translation of the English,
because that is the word a user of that language has been reading for twenty years: *cinta de
opciones* in Spanish, *cinta d'opcions* in Catalan, *cinta de opcións* in Galician, *ruban* in
French, *Menüband* in German, *barra multifunzione* in Italian, *friso* in European Portuguese and
*zinta* in Basque.

Brazilian Portuguese says *faixa de opções* rather than *friso*. If your application ships in
`pt-BR`, use that.
