using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Digi21.WinUI.Ribbon.Primitives;

// What the four item types would inherit if they could.
//
// They cannot: each of them derives from the WinUI control that already gets its behaviour right -
// Button, ToggleButton, DropDownButton - which is what brings the InvokePattern, the TogglePattern
// and the keyboard handling with it. So the shared part is composed instead of inherited, exactly
// as AppBarButton and AppBarToggleButton share ICommandBarElement and nothing else.
//
// Its whole job is to carry the label, the icon and the shape the layout chose into the item's
// template. WinUI has no TemplatedParent to bind through, so the item pushes them in.
internal sealed class RibbonItemChrome
{
    private readonly Control owner;
    private readonly Func<string> label;
    private readonly Func<IconSource?> icon;

    private RibbonItemContent? content;

    internal RibbonItemChrome(Control owner, Func<string> label, Func<IconSource?> icon)
    {
        this.owner = owner;
        this.label = label;
        this.icon = icon;

        // The strip writes Ribbon.Size on the item as it lays out, and the chrome has to follow it
        // within the same measure pass - which is why this is a property callback and not a binding.
        owner.RegisterPropertyChangedCallback(Ribbon.SizeProperty, (_, _) => Update());
    }

    // Called from the item's OnApplyTemplate with whatever the template put under PART_Content.
    internal void Attach(RibbonItemContent? part)
    {
        content = part;
        Update();
    }

    internal void Update()
    {
        // The name an item announces itself by is its label. Without it every button in the ribbon
        // is "button" to a screen reader and to anything trying to drive the application.
        AutomationProperties.SetName(owner, label());

        if (content is null)
        {
            return;
        }

        content.Label = label();
        content.IconSource = icon();
        content.ItemSize = Ribbon.GetSize(owner);
    }
}
