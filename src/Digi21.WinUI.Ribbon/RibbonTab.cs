using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Digi21.WinUI.Ribbon;

/// <summary>One tab of the ribbon: a name in the strip and a row of groups under it.</summary>
/// <remarks>
/// <para>
/// A tab can also come and go. Set <see cref="IsContextual"/> once, declare its groups once, and then
/// tie <see cref="IsActive"/> to whatever state of the application the tab belongs to: the tab is on
/// the strip while that is true and off it while it is not, and the controls inside it are the same
/// objects throughout. That is Office's contextual tab - the table tools that arrive when the caret
/// is in a table - and it is the answer to a set of commands that would otherwise sit on a fixed tab
/// greyed out, saying nothing about when they will work.
/// </para>
/// </remarks>
[ContentProperty(Name = nameof(Groups))]
[TemplatePart(Name = GroupsPart, Type = typeof(RibbonGroupsPanel))]
public partial class RibbonTab : Control
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonTab), new PropertyMetadata(string.Empty, OnChromeChanged));

    /// <summary>Identifies the <see cref="IsContextual"/> dependency property.</summary>
    public static readonly DependencyProperty IsContextualProperty =
        DependencyProperty.Register(nameof(IsContextual), typeof(bool), typeof(RibbonTab), new PropertyMetadata(false, OnChromeChanged));

    /// <summary>Identifies the <see cref="ContextualGroup"/> dependency property.</summary>
    public static readonly DependencyProperty ContextualGroupProperty =
        DependencyProperty.Register(nameof(ContextualGroup), typeof(RibbonContextualGroup), typeof(RibbonTab), new PropertyMetadata(null, OnChromeChanged));

    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(RibbonTab), new PropertyMetadata(true, OnActiveChanged));

    /// <summary>Identifies the <see cref="SelectsWhenActivated"/> dependency property.</summary>
    public static readonly DependencyProperty SelectsWhenActivatedProperty =
        DependencyProperty.Register(nameof(SelectsWhenActivated), typeof(bool), typeof(RibbonTab), new PropertyMetadata(true));

    private const string GroupsPart = "PART_Groups";

    private readonly ObservableCollection<RibbonGroup> groups = [];

    private RibbonGroupsPanel? panel;
    private int rows = RibbonMetrics.MaxRows;

    /// <summary>Initializes a new instance of the <see cref="RibbonTab"/> class.</summary>
    public RibbonTab()
    {
        RibbonThemeResources.Ensure();
        DefaultStyleKey = typeof(RibbonTab);
        groups.CollectionChanged += OnGroupsChanged;
    }

    /// <summary>Gets or sets the name shown in the tab strip, already in the user's language.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether this is a tab that comes and goes. Fixed, out of the box.</summary>
    /// <remarks>
    /// <para>
    /// A contextual tab is marked as one - an accent line above its name, which is where the coloured
    /// heading over a set of them would go - and it steps forward as it arrives, which a fixed tab
    /// hidden and shown again never does. It also says so to a screen reader, through
    /// <see cref="RibbonStrings.ContextualTabNameFormat"/>.
    /// </para>
    /// <para>
    /// What switches it on and off is <see cref="IsActive"/>. This says what kind of tab it is, and
    /// an application sets it once.
    /// </para>
    /// </remarks>
    public bool IsContextual
    {
        get => (bool)GetValue(IsContextualProperty);
        set => SetValue(IsContextualProperty, value);
    }

    /// <summary>Gets or sets the heading this tab is drawn under: Office's coloured band over a set of contextual tabs.</summary>
    /// <remarks>
    /// <para>
    /// The band carries a name and a colour, and it is what says that these tabs go together and
    /// that they are here because of something that has just happened - which a two pixel line above
    /// one tab says to nobody who was not watching the strip at the moment it appeared. Point
    /// several contextual tabs at the same <see cref="RibbonContextualGroup"/> and they are drawn
    /// under one band; point one at it and the band is over that one.
    /// </para>
    /// <para>
    /// It goes with <see cref="IsContextual"/> rather than instead of it: a fixed tab in a group is
    /// drawn like any other fixed tab, because a heading over a tab that is always there is a
    /// heading that says nothing. The room for the band is held from the moment any tab is given a
    /// group, whether or not that tab is switched on, so a tab arriving never changes the height of
    /// the ribbon.
    /// </para>
    /// </remarks>
    public RibbonContextualGroup? ContextualGroup
    {
        get => (RibbonContextualGroup?)GetValue(ContextualGroupProperty);
        set => SetValue(ContextualGroupProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the tab is on the strip. On, out of the box.</summary>
    /// <remarks>
    /// <para>
    /// The two-way property a contextual tab is driven by: tie it to whatever the tab is about - a
    /// selection waiting to be dealt with, a table the caret is in - and the tab arrives and leaves
    /// with it. Nothing is rebuilt on the way. The tab is realized once, when it is added to
    /// <see cref="Ribbon.Tabs"/>, and the controls an application put in its groups are the same
    /// objects every time it comes back.
    /// </para>
    /// <para>
    /// A tab that is not active has no header, so it is not on the strip and UI Automation cannot see
    /// it either. Asking the ribbon to select one leaves the ribbon where it was. If it was the tab
    /// on show when it went, the ribbon goes back to the tab that was showing when this one was
    /// chosen - or to the first tab there is, if that one has gone too.
    /// </para>
    /// <para>
    /// It works on a fixed tab as well, and hides it. What it does not do there is announce itself: a
    /// tab that arrives with no mark on it and does not step forward is one a user finds by noticing
    /// that the strip is a name longer than it was, which is not noticing at all.
    /// </para>
    /// </remarks>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether a contextual tab is shown as it arrives. On, out of the box.</summary>
    /// <remarks>
    /// <para>
    /// On, because a contextual tab appears at the moment its commands start being worth having, and
    /// whoever did the thing that made it appear is usually looking for one of them. Off for the tab
    /// that arrives while the user is in the middle of something else, where taking the strip out
    /// from under them is worse than letting them find it.
    /// </para>
    /// <para>
    /// Only a contextual tab does this, and only when it is switched on after the ribbon has been
    /// built. A tab that is already active when the ribbon is first laid out - declared that way in
    /// XAML - is simply on the strip: at startup nothing has just happened, and the ribbon opens on
    /// the tab the application opens on.
    /// </para>
    /// <para>
    /// A fixed tab switched back on with <see cref="IsActive"/> stays where it is put, whatever this
    /// says.
    /// </para>
    /// </remarks>
    public bool SelectsWhenActivated
    {
        get => (bool)GetValue(SelectsWhenActivatedProperty);
        set => SetValue(SelectsWhenActivatedProperty, value);
    }

    /// <summary>Gets the groups of this tab, left to right.</summary>
    /// <remarks>The order here is the order on screen; which of them gives way first is <see cref="RibbonGroup.Priority"/>, not this.</remarks>
    public IList<RibbonGroup> Groups => groups;

    // The ribbon this tab has been added to, or null while it belongs to nobody.
    //
    // A reference the ribbon writes when it takes the tab, and not a walk up the visual tree, which
    // the rest of this library refuses to do for a reason that holds here too: a minimised ribbon
    // moves its body into a popup, and a tab reading its way up would find the popup. This says who
    // owns the tab rather than where it happens to be drawn.
    internal Ribbon? Owner { get; set; }

    // Where the ribbon goes back to if this tab is switched off while it is the one on show.
    //
    // Recorded when the tab is chosen and not when it arrives, which is the difference between "the
    // tab you came from" and "the tab you were on some time ago": a contextual tab that arrives
    // without taking the strip, is left alone for a while and is then clicked, goes back to wherever
    // the user actually was.
    internal RibbonTab? Restore { get; set; }

    // The rows the groups of this tab are laid out in, handed down from the ribbon and handed on to
    // the groups. Down a chain rather than read back up one, because a minimised ribbon moves its
    // body into a popup and a group reading its way up to the ribbon would find the popup instead.
    internal int Rows
    {
        get => rows;

        set
        {
            rows = value;

            foreach (RibbonGroup group in groups)
            {
                group.Rows = value;
            }
        }
    }

    // How tall this tab needs to be: the group that needs the most. Asked of every tab by the ribbon,
    // whether or not it is the one showing, so that choosing a tab never changes the height of the
    // strip an application has put its whole window under.
    //
    // Every tab includes the contextual ones that are switched off, and that is a trade taken on
    // purpose. A ribbon that grew as a contextual tab arrived would push the window down at the
    // moment somebody was reaching for a command in it - the same fault choosing a tab used to cause,
    // no better for having a different trigger. What it costs is that a contextual tab holding a
    // stack of controls makes the ribbon that tall from the start, whether or not it is ever shown.
    internal double RequiredHeight
    {
        get
        {
            double height = 0;

            foreach (RibbonGroup group in groups)
            {
                height = Math.Max(height, group.RequiredHeight);
            }

            return height;
        }
    }

    // Measures this tab now, while it still can be, so that it can say how tall it needs to be for
    // the rest of its life.
    //
    // The ribbon calls this on a tab it is about to put away. What a group needs is arithmetic over
    // the natural heights of the items in it, and those can only be read while the tab holding them
    // is visible - so a tab that is never chosen gets exactly one chance to be measured, and this is
    // it. A tab declared in XAML has it long before anything is collapsed, on the pass that runs
    // before the ribbon even has a template. A tab built from code arrives into a ribbon that is
    // already in the tree and is put away in the same turn, and this is the whole of the moment.
    //
    // Measured whole rather than group by group, which is the difference between a number and the
    // right number. Measuring the tab applies its template, puts its groups into the panel that lays
    // them out and runs the same measure the strip runs, so every group ends up holding what it
    // would have held had it been on show. Asked directly, a group reaches down into parts WinUI has
    // never realized: a NumberBox with no template measures as a fraction of itself - sixty-one
    // pixels for a stack of three that is a hundred - and every one of those fractions is a number
    // the ribbon would then be exactly as tall as.
    //
    // Against no width, because what it needs is what it needs with every group open. Which of them
    // fold is a question about a width, and it is asked again every time the strip is laid out.
    internal void EnsureMeasured()
    {
        // Put back on for the length of one call, because a collapsed element is not measured at
        // all - the layout system skips it, and every control under it that has never been given a
        // template stays that way. Uncollapsing it here rather than relying on catching it before it
        // is put away is what lets this be asked at any time: an application that adds a tab and
        // then fills it, or that adds a group to a tab nobody is looking at, has changed how tall
        // the ribbon needs to be and is entitled to be believed rather than measured next time
        // somebody happens to click on it.
        //
        // Nothing sees the difference. It is set back before this returns, and no layout pass, no
        // hit test and no automation client runs in between.
        Visibility was = Visibility;

        Visibility = Visibility.Visible;
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Visibility = was;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        panel = GetTemplateChild(GroupsPart) as RibbonGroupsPanel;
        Sync();
    }

    private static void OnChromeChanged(DependencyObject tab, DependencyPropertyChangedEventArgs arguments)
    {
        RibbonTab self = (RibbonTab)tab;
        self.Owner?.OnTabChromeChanged(self);
    }

    private static void OnActiveChanged(DependencyObject tab, DependencyPropertyChangedEventArgs arguments)
    {
        // Told to the ribbon rather than acted on here. What a tab arriving or leaving means is a
        // question about the strip as a whole - which headers there are, which tab is showing
        // afterwards - and only the ribbon can answer it. A tab that belongs to nobody yet has
        // nothing to tell: the ribbon reads the property when it takes the tab.
        RibbonTab self = (RibbonTab)tab;
        self.Owner?.OnTabActivationChanged(self);
    }

    private void OnGroupsChanged(object? sender, NotifyCollectionChangedEventArgs arguments)
    {
        Sync();

        // A tab that is on show is measured by the pass this change has just asked for. A tab that
        // is not is never measured again, so what it says it needs would be what it needed before
        // this group arrived - and an application that generates its ribbon from its own command
        // registry adds tabs and then fills them.
        if (Visibility == Visibility.Collapsed)
        {
            EnsureMeasured();
            Owner?.OnTabHeightChanged();
        }
    }

    private void Sync()
    {
        if (panel is null)
        {
            return;
        }

        for (int i = panel.Children.Count - 1; i >= 0; i--)
        {
            if (panel.Children[i] is RibbonGroup existing && !groups.Contains(existing))
            {
                panel.Children.RemoveAt(i);
            }
        }

        foreach (RibbonGroup group in groups)
        {
            // Here as well as in the setter, so that a group added to a tab later is laid out in the
            // rows the ribbon is in rather than in the three it was born assuming.
            group.Rows = rows;

            if (!panel.Children.Contains(group))
            {
                panel.Children.Add(group);
            }
        }

        panel.InvalidateMeasure();
    }
}
