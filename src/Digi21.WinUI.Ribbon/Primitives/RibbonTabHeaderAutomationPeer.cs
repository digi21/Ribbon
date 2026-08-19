using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>Makes a tab announce itself as one of a set of tabs, and lets one be chosen without a mouse.</summary>
/// <remarks>
/// <para>
/// Written because the probe found it missing: the header derives from <c>ButtonBase</c>, which
/// unlike <c>Button</c> brings no peer of its own, so every tab of the ribbon answered to no pattern
/// at all. An application on top could not choose a tab except by clicking a coordinate, which is
/// the failure this library exists to avoid one layer down.
/// </para>
/// <para>
/// Both patterns, because both are true: invoking a tab is what a driver reaches for, and being one
/// of a set exactly one of which is chosen is what a tab is.
/// </para>
/// </remarks>
public partial class RibbonTabHeaderAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, ISelectionItemProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonTabHeaderAutomationPeer"/> class.</summary>
    /// <param name="owner">The tab this peer speaks for.</param>
    public RibbonTabHeaderAutomationPeer(RibbonTabHeader owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public bool IsSelected => Header.IsSelected;

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer => null;

    private RibbonTabHeader Header => (RibbonTabHeader)Owner;

    /// <inheritdoc/>
    public void Invoke()
    {
        Header.Choose();
    }

    /// <inheritdoc/>
    public void Select()
    {
        Header.Choose();
    }

    /// <inheritdoc/>
    public void AddToSelection()
    {
        Header.Choose();
    }

    /// <summary>Not supported: a ribbon always has a tab showing.</summary>
    /// <exception cref="InvalidOperationException">Always.</exception>
    public void RemoveFromSelection() =>
        throw new InvalidOperationException("A ribbon always shows one tab, so a tab cannot be deselected on its own.");

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonTabHeader);

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface) =>
        patternInterface is PatternInterface.Invoke or PatternInterface.SelectionItem
            ? this
            : base.GetPatternCore(patternInterface);
}
