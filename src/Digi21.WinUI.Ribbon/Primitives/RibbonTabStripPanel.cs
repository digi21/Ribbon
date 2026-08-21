using Digi21.WinUI.Ribbon.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>The strip of tab names, and the coloured bands drawn above the contextual ones.</summary>
/// <remarks>
/// <para>
/// A row of headers, left to right in the order they were declared, with a band over each set of
/// contextual tabs that share a <see cref="RibbonContextualGroup"/>. It is a panel of its own rather
/// than a stack and a canvas because the two halves have to agree: where a band starts and ends is
/// where its first and last tab start and end, and only the thing that placed the tabs knows that.
/// </para>
/// <para>
/// The room for the bands is held whether or not any band is being drawn. What comes and goes is
/// <see cref="RibbonTab.IsActive"/>, many times a minute, and a strip that changed height as a tab
/// arrived would push the whole window down at the moment somebody was reaching into it. A ribbon
/// with no contextual group at all holds nothing and is exactly as tall as it was before there were
/// bands to draw.
/// </para>
/// </remarks>
public sealed partial class RibbonTabStripPanel : Panel
{
    /// <summary>The gap between one tab name and the next.</summary>
    /// <remarks>Here rather than in the template, because a band has to span the gaps between the tabs it covers and cannot ask the template what they are.</remarks>
    internal const double Spacing = 2;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        Strip strip = Read(measure: true);

        return new Size(strip.Width, strip.Bands + strip.Tabs);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // Worked out again rather than kept from the measure pass. It is the same arithmetic over
        // the same numbers, so the two passes cannot disagree - and nothing here is then holding a
        // list of which tabs were on the strip the last time anybody asked, which is a list that
        // goes stale the moment one of them is switched on.
        Strip strip = Read(measure: false);

        for (int i = 0; i < strip.Headers.Count; i++)
        {
            strip.Headers[i].Arrange(new Rect(
                strip.Lefts[i],
                strip.Bands,
                strip.Widths[i],
                Math.Max(0, finalSize.Height - strip.Bands)));
        }

        foreach (RibbonContextualHeading heading in strip.Headings)
        {
            (double left, double width) = strip.Cover(heading);

            // A band whose tabs have all been switched off is arranged as nothing rather than
            // collapsed: collapsed it would measure as nothing too, and the room held for it - which
            // is the whole reason a tab arriving does not move the window - would go with it.
            heading.Arrange(new Rect(left, 0, width, strip.Bands));
        }

        return finalSize;
    }

    // The row of tabs and the bands above it, as they stand now.
    private Strip Read(bool measure)
    {
        var headers = new List<RibbonTabHeader>();
        var headings = new List<RibbonContextualHeading>();

        foreach (UIElement child in Children)
        {
            switch (child)
            {
                case RibbonContextualHeading heading:
                    headings.Add(heading);
                    break;

                case RibbonTabHeader { Visibility: Visibility.Visible } header:
                    headers.Add(header);
                    break;

                default:
                    break;
            }
        }

        var lefts = new double[headers.Count];
        var widths = new double[headers.Count];

        double width = 0;
        double tabs = 0;
        double bands = 0;

        for (int i = 0; i < headers.Count; i++)
        {
            if (measure)
            {
                // Against no ceiling: a tab name is as wide as it is, and a strip too narrow for its
                // names overflows rather than trimming them. Office does the same, and it is the
                // lesser evil - a tab whose name is cut is a tab nobody can identify.
                headers[i].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            lefts[i] = width;
            widths[i] = headers[i].DesiredSize.Width;

            width += widths[i] + Spacing;
            tabs = Math.Max(tabs, headers[i].DesiredSize.Height);
        }

        foreach (RibbonContextualHeading heading in headings)
        {
            if (measure)
            {
                // How much room a band needs, asked of every one of them and not only of the ones
                // being drawn. That is what holds the height: a group whose tabs are all switched
                // off is measured exactly like one whose tabs are on the strip.
                heading.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            bands = Math.Max(bands, heading.DesiredSize.Height);
        }

        var strip = new Strip(headers, headings, lefts, widths, Math.Max(0, width - Spacing), tabs, bands);

        if (measure)
        {
            // And then each band against the tabs it actually covers, which is what makes it exactly
            // as wide as they are. Measured against nothing it keeps the width of its own name - too
            // wide for one tab, and then drawn at that width whatever it is arranged at, because an
            // element is never rendered smaller than it asked to be.
            foreach (RibbonContextualHeading heading in headings)
            {
                heading.Measure(new Size(strip.Cover(heading).Width, bands));
            }
        }

        return strip;
    }

    private sealed record Strip(
        IReadOnlyList<RibbonTabHeader> Headers,
        IReadOnlyList<RibbonContextualHeading> Headings,
        double[] Lefts,
        double[] Widths,
        double Width,
        double Tabs,
        double Bands)
    {
        // Where a band lies: from the left edge of the first tab of its group to the right edge of
        // the last, the gaps between them included, because what it says is that they are one thing.
        internal (double Left, double Width) Cover(RibbonContextualHeading heading)
        {
            if (heading.Group is not { } group)
            {
                return (0, 0);
            }

            var groups = new object?[Headers.Count];

            for (int i = 0; i < Headers.Count; i++)
            {
                groups[i] = Headers[i].Group;
            }

            (int first, int count) = RibbonHeadingSpan.Of(groups, group);

            if (count == 0)
            {
                return (0, 0);
            }

            int last = first + count - 1;

            return (Lefts[first], Lefts[last] + Widths[last] - Lefts[first]);
        }
    }
}
