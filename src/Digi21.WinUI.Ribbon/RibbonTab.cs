using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Digi21.WinUI.Ribbon.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Digi21.WinUI.Ribbon;

/// <summary>One tab of the ribbon: a name in the strip and a row of groups under it.</summary>
[ContentProperty(Name = nameof(Groups))]
[TemplatePart(Name = GroupsPart, Type = typeof(RibbonGroupsPanel))]
public partial class RibbonTab : Control
{
    /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(RibbonTab), new PropertyMetadata(string.Empty));

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

    /// <summary>Gets the groups of this tab, left to right.</summary>
    /// <remarks>The order here is the order on screen; which of them gives way first is <see cref="RibbonGroup.Priority"/>, not this.</remarks>
    public IList<RibbonGroup> Groups => groups;

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

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        panel = GetTemplateChild(GroupsPart) as RibbonGroupsPanel;
        Sync();
    }

    private void OnGroupsChanged(object? sender, NotifyCollectionChangedEventArgs arguments)
    {
        Sync();
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
