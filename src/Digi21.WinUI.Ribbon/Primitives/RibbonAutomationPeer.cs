using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Digi21.WinUI.Ribbon.Primitives;

/// <summary>Makes the ribbon announce itself as the set of tabs its headers say they belong to.</summary>
/// <remarks>
/// <para>
/// The headers have called themselves tab items since the probe found them answering to no pattern
/// at all. A tab item with no set above it is still half an answer: a driver can invoke one, and
/// cannot ask which tabs there are or which of them is showing without walking the tree and
/// recognising the pieces by type. This is the other half, and it arrives with contextual tabs
/// because that is when the question stops being tidiness - the set of tabs is now something that
/// changes while the application runs, and a driver has to be able to read it rather than remember
/// it.
/// </para>
/// <para>
/// It reports one selection and requires one, which is what a ribbon does. The exception is a ribbon
/// every tab of which is contextual and none of them switched on yet: there is nothing to report
/// then, and <see cref="GetSelection"/> says so with an empty answer rather than with a tab that is
/// not on the strip.
/// </para>
/// </remarks>
public partial class RibbonAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonAutomationPeer"/> class.</summary>
    /// <param name="owner">The ribbon this peer speaks for.</param>
    public RibbonAutomationPeer(Ribbon owner)
        : base(owner)
    {
    }

    /// <summary>Gets a value indicating whether several tabs can be chosen at once. They cannot.</summary>
    public bool CanSelectMultiple => false;

    /// <summary>Gets a value indicating whether a tab has to be chosen. One does.</summary>
    public bool IsSelectionRequired => true;

    private Ribbon Ribbon => (Ribbon)Owner;

    /// <summary>Gets the tab on show, as the one member of the selection.</summary>
    /// <returns>The header of the tab on show, or nothing when no tab is on the strip.</returns>
    public IRawElementProviderSimple[] GetSelection() =>
        Ribbon.SelectedHeader is { } header
        && (FromElement(header) ?? CreatePeerForElement(header)) is { } peer
            ? [ProviderFromPeer(peer)]
            : [];

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(Ribbon);

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface) =>
        patternInterface is PatternInterface.Selection
            ? this
            : base.GetPatternCore(patternInterface);
}
