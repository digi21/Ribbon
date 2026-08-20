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

## What this version does not do

There is no contextual tab *group*: Office's coloured heading spanning several tabs at once
("Table Tools"), with each group in its own colour. A single contextual tab is the whole feature
here.

The shape is left open for it rather than closed against it. The accent line is drawn along the
**top** edge of a header and edge to edge rather than inset, so two contextual tabs side by side
already draw one unbroken line — which is what a heading above them would be underlined by. Tabs are
never reordered, so a set of contextual tabs declared together stays contiguous in the strip, which
is what a heading would have to span. Adding the group later means adding a row above the strip and a
brush per group; it does not mean revisiting `IsActive`, the ordering, or the selection rule.
