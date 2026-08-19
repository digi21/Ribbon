# How the ribbon decides what fits

The visible promise is that a ribbon narrows gracefully: items step down through their shapes, then a
group folds into a button, and nothing ever leaves the strip. This is how that is arrived at, and why
it is arrived at in this particular way.

It is worth reading before changing anything in `Layout/`, and worth reading if you are wondering why
your groups gave way in the order they did.

## The three shapes

An item is drawn in one of three shapes, and declares which of them it will accept:

| Shape | Is | Takes |
| --- | --- | --- |
| `Large` | The icon above the label | A column of its own, the full height of the group |
| `Normal` | The icon and the label side by side | One row |
| `Small` | The icon alone | One row |

```xml
<ribbon:RibbonButton Label="Cut" ribbon:Ribbon.AllowedSizes="Normal,Small" />
```

An item that accepts nothing small enough for the room left keeps the smallest shape it does accept.
A hosted `NumberBox` declaring `Normal` alone stays that size however hard the ribbon is squeezed,
which is what stops the layout from believing it has recovered width it has not.

An item that declares nothing — a control dropped straight into a group — is laid out as `Normal`.

## Columns of three

A group packs its items into columns of at most three rows, as Office does. Items taking one row fill
a column top to bottom and then start another; an item drawn `Large`, and a separator, take a column
to themselves. A column is as wide as the widest item in it, which is why one long label in a column
of three widens all three.

A group is never narrower than its own name, and the name is the whole of the floor — with the
launcher beside it when the group has one.

## Giving way

Each group has a **cap** — `Large`, `Normal` or `Small` — and each of its items takes the largest
shape it accepts that is no bigger than the cap. Three states per group, not a combinatorial search.

The strip then walks a sequence of arrangements, each a degradation of the one before:

1. Lower the cap of the group with the **lowest priority** that can still be lowered. Ties are broken
   from the right, as in Office.
2. When no cap can go lower, **fold** the lowest-priority group into its button — but only if it is
   actually narrower folded. A group of two icons is wider as a button carrying its own name than it
   is as itself, and folding it would widen the strip and hide two commands.
3. When no group is worth folding, take the **name off** a folded button, one button at a time and in
   the same order. A folded button without its name is the least identifiable state there is, so it
   is the last thing tried and never a rung among the others.

```xml
<ribbon:RibbonGroup Label="Clipboard" Priority="0" />   <!-- gives way first -->
<ribbon:RibbonGroup Label="Font"      Priority="10" />
<ribbon:RibbonGroup Label="Paragraph" Priority="20" />  <!-- gives way last -->
```

`Priority` defaults to zero, so a ribbon that declares nothing loses room from the right.

There is no state after the last one. If the strip still does not fit, every group is on it, in its
button, and the ribbon clips: a command drawn off the edge can be reached by widening the window, and
one that has been taken out of the strip cannot be reached at all.

## Why it cannot flicker

The sequence above is generated **without reference to the width available**. Which group gives way
next is read from the state alone — the caps, what has folded, the priorities — so the sequence is
the same at every width. The width only chooses where in it to stop, at the first arrangement that
fits.

That separation is enforced by the code and not by a comment: `RibbonLayoutSolver.States` does not
receive the width, and `Solve` takes the first state that fits. Making the choice depend on the
available width is a compile error rather than a bug.

Everything the ribbon promises follows from those two sentences:

- **Nothing grows as the window narrows.** A narrower window stops at the same state or a later one,
  and every state it walked past was — by the test that made it walk past — wider than the width it
  had. So the state it lands on is narrower than the one a wider window stopped at.
- **Narrowing and widening back lands exactly where it started**, because the stopping point is a
  function of the width and of nothing else.
- **No width admits two arrangements**, so there is nothing to flicker between. A layout that decided
  from the room it had *left over* would feed its own output into its input: folding a group frees
  width, the freed width invites the group back, and the two take turns at the width where one
  becomes the other.

## Measured, not assumed

The layout is arithmetic over numbers the control measured. Only a live element knows how wide its
label renders at the current scale and font, so the control measures each item in each of the three
shapes and hands those numbers over; everything after that is plain logic over plain records, which
is why it is tested without a window.

The rule that keeps this honest is that **the numbers the layout decides with must be the numbers the
control draws with**. Two faults in this library's history were breaches of it, and both were
invisible: a group predicted narrower than it rendered, quietly clipped, with nothing looking wrong.
The first was a group name the layout did not count; the second was a launcher.

If you add anything that takes room in a group, it has to reach `RibbonGroupMetrics`, and the
regression harness has a check that a strip may only overflow at the last arrangement of its
sequence — which is what caught the second one.
