using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Ribbon;

/// <summary>What every item the ribbon draws itself has in common: something to say and something to show.</summary>
/// <remarks>
/// <para>
/// The item types have no base class between them: <see cref="RibbonButton"/> is a
/// <see cref="Button"/>, <see cref="RibbonToggleButton"/> is a
/// <see cref="Microsoft.UI.Xaml.Controls.Primitives.ToggleButton"/>, and each of them therefore
/// arrives with the click behaviour, the keyboard handling and the automation peer that WinUI
/// already gets right - an <c>InvokePattern</c> on the one and a <c>TogglePattern</c> on the other,
/// which is what lets an application built on this ribbon be driven by a test rather than by screen
/// coordinates. This interface is what they share instead, the way <c>ICommandBarElement</c> is what
/// <c>AppBarButton</c> and <c>AppBarToggleButton</c> share.
/// </para>
/// <para>
/// It is here so that an application walking its own ribbon to find a control does not need a
/// <c>switch</c> over four types to read a label.
/// </para>
/// <para>
/// A group can hold any <see cref="Microsoft.UI.Xaml.UIElement"/> at all, not only these: a
/// <c>NumberBox</c> dropped straight into <see cref="RibbonGroup.Items"/> is laid out as
/// <see cref="RibbonItemSize.Normal"/> and keeps its focus, with nothing to declare and nothing to
/// wrap it in.
/// </para>
/// </remarks>
public interface IRibbonItem
{
    /// <summary>Gets or sets the text shown beside or below the icon, already in the user's language.</summary>
    /// <remarks>The ribbon does not translate what an application puts in it; see the library's own strings for what it does translate.</remarks>
    string Label { get; set; }

    /// <summary>Gets or sets the recipe the icon is built from.</summary>
    /// <remarks>
    /// A source rather than an <see cref="IconElement"/> because an item is drawn at more than one
    /// size: an element is one instance with one size and one parent, and a source makes the element
    /// each shape needs. It also carries the monochrome tinting that keeps an icon visible when the
    /// application switches to a dark theme.
    /// </remarks>
    IconSource? IconSource { get; set; }
}
