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

    /// <summary>Gets the ribbon this tab is one of.</summary>
    /// <remarks>
    /// It was <see langword="null"/> to begin with, which says "this is a tab of nothing" - and a
    /// driver reading a contextual tab has one question beyond its name, which is what set it belongs
    /// to and what else is in it. Answering that is what the ribbon's own peer is for.
    /// </remarks>
    public IRawElementProviderSimple? SelectionContainer =>
        Header.Owner is { } ribbon
        && (FromElement(ribbon) ?? CreatePeerForElement(ribbon)) is { } peer
            ? ProviderFromPeer(peer)
            : null;

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

    /// <summary>Not supported: a ribbon shows the tab that is chosen, and choosing another is how one stops being chosen.</summary>
    /// <exception cref="InvalidOperationException">Always.</exception>
    public void RemoveFromSelection() =>
        throw new InvalidOperationException("A ribbon shows the tab that is chosen, so a tab cannot be deselected on its own.");

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
