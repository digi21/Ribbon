# Contextual tabs

A tab that is on the strip only while it is worth having, and gone the rest of the time. Office's
table tools, appearing when the caret is in a table; or an editor's *what to do with what you just
selected*, appearing when a selection is sitting there waiting for a decision.

The alternative it exists to replace is a fixed tab whose commands are switched off most of the time.
That is cheaper and worse: a greyed-out button says that something is unavailable, but it never says
*when* it will be available, and it certainly does not say that the moment has just arrived. A tab
that appears at that moment says both.

## In one screenful

```csharp
var table = new RibbonTab
{
    Label = "Table",
    IsContextual = true,
    IsActive = false,
};

table.Groups.Add(commands);
ribbon.Tabs.Add(table);

// ... and from then on, this is the whole of it:
table.IsActive = document.CaretIsInATable;
```

In XAML the same tab, with the flag bound to whatever the application knows:

```xml
<ribbon:RibbonTab Label="Table" IsContextual="True" IsActive="{x:Bind IsCaretInATable, Mode=OneWay}">
    <ribbon:RibbonGroup Label="Rows and columns">
        <ribbon:RibbonButton Label="Insert row" />
        <ribbon:RibbonButton Label="Insert column" />
        <ribbon:RibbonButton Label="Merge cells" />
        <ribbon:RibbonButton Label="Delete table" />
    </ribbon:RibbonGroup>
</ribbon:RibbonTab>
```

`IsContextual` says what kind of tab it is and is set once. `IsActive` says whether it is on the
strip now, and it is an ordinary two-way dependency property — bind it, or write to it, or read it
back; the ribbon never writes to it itself.

## The three properties

| Property | Default | Says |
| --- | --- | --- |
| `RibbonTab.IsContextual` | `false` | That this tab comes and goes: it is marked with an accent line, it announces itself to a screen reader as contextual, and it steps forward when it arrives. |
| `RibbonTab.IsActive` | `true` | Whether the tab is on the strip. The one an application drives. |
| `RibbonTab.SelectsWhenActivated` | `true` | Whether a contextual tab becomes the tab on show as it arrives. |

`IsActive` works on a fixed tab too, and hides it. What a fixed tab does not do is announce itself or
step forward — so use `IsContextual` for a tab a user is meant to *notice*, and a bare `IsActive` for
one that should simply not be offered.

## What is decided, and what happens when

**It appears where it was declared.** The strip draws `Tabs` in order, and a contextual tab reappears
in the gap it left rather than being moved to the end. Declare it last if it should sit on the right,
as Office puts them. This keeps the visual order and the collection order the same thing, so "the
third tab" means one thing in the code, in the documentation and in a test.

**`SelectedIndex` still indexes `Tabs`, all of it.** A tab switched off does not shift the ones after
it. What it does do is become unselectable: setting `SelectedIndex` to an inactive tab leaves the
ribbon where it was and puts the property back, rather than throwing or showing nothing. The set of
tabs on the strip changes while an application runs, so asking for one that has just gone is a race,
not a mistake.

**It steps forward as it arrives, unless told not to.** `SelectsWhenActivated` is on by default,
because a contextual tab appears at the moment its commands start working and whoever caused that is
usually reaching for one of them. Switch it off for the tab that arrives while the user is in the
middle of something else.

**Starting active is not arriving.** A tab whose `IsActive` is already `true` when the ribbon is
first built — declared that way in XAML, or set before the window has been laid out — is simply on
the strip. It does not step forward and it raises no `TabActivated`, because at startup nothing has
just happened and the ribbon should open on the tab an application opens on. Stepping forward is
what a tab does when it is switched on in answer to something the user did. In practice the line is
where you would draw it anyway: set in the constructor, it is initial state; set in a handler, it is
an arrival.

**When it goes, the ribbon goes back where the user came from.** Precisely: back to the tab that was
showing when the contextual tab was *chosen* — not when it appeared. A contextual tab that arrives
quietly, is ignored for a while and is then clicked returns to wherever the user actually was, not to
where they were several minutes ago. Two contextual tabs in a row unwind in order: the second goes
back to the first. If the tab it would return to has gone too, the ribbon falls to the first tab
there is. If *every* tab is off the strip, `SelectedIndex` reads `-1`, `SelectedTab` is `null`, and
the ribbon draws an empty strip rather than a tab nobody offered.

**The ribbon does not change height when one arrives.** Every tab pays into the ribbon's single
height, including the contextual ones that are switched off. A ribbon that grew as a contextual tab
appeared would push the whole window down at exactly the moment somebody was reaching for a command
in it. The cost is the honest one: a contextual tab holding a stack of controls makes the ribbon that
tall from the start, whether or not it is ever shown.

**`DisplayMode` and `IsMinimized` treat it as any other tab.** A simplified ribbon lays a contextual
tab out in one row like the rest. A minimised ribbon shows its header in the strip, and that header
appearing is the signal — the ribbon does *not* open itself over the content unasked, because a user
who put the ribbon away asked for the content, not for a panel to arrive over it. Clicking the header
opens it over the content as any other tab does. If the ribbon is open over the content showing a
contextual tab and that tab is switched off, the overlay closes with it rather than swapping in a tab
nobody asked to see.

**Nothing is rebuilt.** The tab is realized once, when it is added to `Tabs`, and stays realized.
`IsActive` takes away its header, not the tab: the groups, the items and the application's references
to them all survive it being off the strip. A tab that comes and goes twenty times a minute costs one
build and no more — which is the promise the whole library is built on, and it applies here without
an exception.

## Automation

The point is that a test can find out what happened without polling the visual tree.

- Every tab header answers to `TabItem`, with `InvokePattern` and `SelectionItemPattern`, contextual
  or not. A contextual tab's `AutomationProperties.Name` is `RibbonStrings.ContextualTabNameFormat`
  around its label — `"Table, contextual tab"` out of the box, and in nine languages in
  [localisation.md](localisation.md).
- A tab that is not active has a **collapsed** header, which puts it out of the automation tree
  entirely. A driver cannot find it, rather than finding it and being unable to press it.
- The ribbon itself now answers to `Tab` with `SelectionPattern`, through `RibbonAutomationPeer`.
  `GetSelection` hands back the header of the tab on show, and `SelectionContainer` on a header hands
  back the ribbon. That is how a driver asks which tabs there are and which one is showing, instead
  of walking the tree and recognising the pieces by type.
- In process, `Ribbon.TabActivated` and `Ribbon.TabDeactivated` are raised **after** the strip has
  been rebuilt and after any move to the new tab, so a handler asking `SelectedTab` is told where the
  ribbon ended up.
- Out of process, the same news arrives as a UI Automation `StructureChanged` event on the ribbon,
  and a tab taking the strip raises `SelectionItemPatternOnElementSelected` on its header. Both are
  raised only when something is listening.

## The heading over a set of them

A two pixel accent line says *this tab is contextual* to somebody who is looking at the strip at the
moment it appears. It says nothing to anybody else, and it says nothing at all about which tabs
belong together. Office's answer is a coloured band above them carrying a name — Table Tools, Picture
Tools — and that is `RibbonContextualGroup`:

```xml
<Grid.Resources>
    <ribbon:RibbonContextualGroup x:Key="PictureTools" Label="Picture Tools" Accent="#C55A11" />
</Grid.Resources>

<ribbon:RibbonTab Label="Picture" IsContextual="True" ContextualGroup="{StaticResource PictureTools}" IsActive="{x:Bind ...}" />
<ribbon:RibbonTab Label="Format"  IsContextual="True" ContextualGroup="{StaticResource PictureTools}" IsActive="{x:Bind ...}" />
```

or, built from code:

```csharp
var tools = new RibbonContextualGroup { Label = "Picture tools", Accent = brush };

picture.ContextualGroup = tools;
format.ContextualGroup = tools;
```

Any number of tabs can point at one group, and one tab is a perfectly ordinary case: the band is then
over that tab alone, which is what Office draws for a lone contextual tab too. **A WinUI 3 `Window`
has no `Resources`**, so in XAML the group goes on the root element or in `App.xaml` rather than on
the window.

**One brush does the whole heading.** The band behind the name, the tint behind the tabs of the
group, and the line along the top of each of them all come from `Accent`. The band and the tint are
drawn from it at `RibbonContextualTintOpacity`, so what to hand over is a saturated colour — the same
one the line is drawn in at full strength. Leave `Accent` unset and the group takes
`RibbonContextualTabAccentBrush`, which is the ribbon's own accent colour.

The tint is the part that matters most in practice. The band says what the tabs are for; the tint is
what makes a contextual tab tell itself apart from a fixed one at a glance, at any moment, rather
than only in the second it arrives.

### What it does to the height

Nothing, ever. The room for the band is held from the moment **any** tab is given a group, whether or
not that tab is switched on — so the strip is exactly as tall with the band drawn as without it, and
a tab arriving fills room that was already there.

That is the whole reason it is done that way. `IsActive` is what changes many times a minute; a strip
that grew as a tab arrived would push the window down at the moment somebody was reaching into it,
which is the fault contextual tabs exist to avoid. A ribbon with no contextual group at all holds no
room and is exactly as tall as it was before there were bands to draw.

### Where it is drawn

From the left edge of the first tab of its group to the right edge of the last, gaps between them
included, and above the names rather than across them. As tabs of the group are switched off the band
shrinks onto what is left; with none of them on the strip it is drawn over nothing at all.

Tabs are never reordered, so a set of contextual tabs declared together stays together. A **fixed**
tab declared between two tabs of one group ends up under the band as well: that is the honest picture
of what was declared, and Office does not offer the arrangement at all. Declare the tabs of a group
next to each other.

### What a screen reader is told

A tab in a group announces itself through `RibbonStrings.ContextualTabInGroupNameFormat`, which takes
the tab's name and then the heading's — `"Picture, contextual tab, Picture tools"` out of the box, and
in nine languages in [localisation.md](localisation.md). A tab that announced only its own name would
leave out exactly the half the band adds, and the band itself is out of the automation tree, because
saying it twice is worse than saying it once for somebody who cannot see that the two are the same
thing.

## What this version does not do

Office offers a fixed palette of heading colours and an application picks one. Here the application
brings its own brush, which is less API to learn and one less list to keep in step with a theme.
