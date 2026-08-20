using Digi21.WinUI.Ribbon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RibbonGallery;

public sealed partial class MainWindow : Window
{
    // The whole point of the library, in one field. It is taken once, when the ribbon is built, and
    // it stays good: through the group folding, through a change of tab, through the ribbon being
    // put away and opened over the content. Nothing in this file ever looks it up again.
    private readonly RibbonDropDownButton paste;
    private readonly NumberBox size;

    private int renames;
    private int added;

    public MainWindow()
    {
        InitializeComponent();

        Title = "Digi21.WinUI.Ribbon gallery";
        paste = Paste;
        size = FontSize;

        // Following the property rather than the click, because the button below is not the only way
        // to put the ribbon away: the chevron in the tab strip and Ctrl+F1 do it too, and a button
        // that then offers to do what has already been done is a button nobody can use without
        // looking. IsMinimized being an ordinary dependency property is what makes this one line.
        Ribbon.RegisterPropertyChangedCallback(Digi21.WinUI.Ribbon.Ribbon.IsMinimizedProperty, (_, _) => ShowMinimizedState());
        ShowMinimizedState();

        // And the same for the mode, for the same reason: out of the box the chevron in the corner
        // is what simplifies the ribbon, so this switch is not the only way here either.
        Ribbon.RegisterPropertyChangedCallback(
            Digi21.WinUI.Ribbon.Ribbon.DisplayModeProperty,
            (_, _) => Simplified.IsOn = Ribbon.DisplayMode == RibbonDisplayMode.Simplified);
    }

    // Used by the screenshot run, which drives the selector rather than the root so that the picture
    // does not show a window in one theme with a box beside it saying another.
    internal void SetThemeForPicture(ElementTheme theme)
    {
        Theme.SelectedIndex = theme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0,
        };
    }

    private void ShowMinimizedState()
    {
        MinimizeButton.Content = Ribbon.IsMinimized ? "Bring the ribbon back" : "Put the ribbon away";
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs arguments)
    {
        // The ribbon follows the theme of the tree it is in, and tints its icons from the colour of
        // the text beside them, so switching here is all there is to it.
        Root.RequestedTheme = Theme.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    private void OnMinimize(object sender, RoutedEventArgs arguments)
    {
        Ribbon.IsMinimized = !Ribbon.IsMinimized;
    }

    private void OnSimplified(object sender, RoutedEventArgs arguments)
    {
        // Office's simplified ribbon: one row, no group names, and whatever cannot be drawn in a row
        // - the size box with its label, here - as the button its group folds into. The controls are
        // the same objects on the way there and on the way back; the switch below still writes to
        // them.
        Ribbon.DisplayMode = Simplified.IsOn ? RibbonDisplayMode.Simplified : RibbonDisplayMode.Full;
    }

    private void OnRename(object sender, RoutedEventArgs arguments)
    {
        renames++;

        // Two things at once, and on purpose. A label is invisible on an item the layout has taken
        // down to its icon, and invisible again on a group that has folded, so a demonstration that
        // only renamed something would have nothing to show at exactly the widths worth trying. The
        // hosted box is a control of the application's own, reached through the same kind of field.
        paste.Label = renames % 2 == 1 ? "Pasted!" : "Paste";
        size.Value = 12 + renames;

        RenameResult.Text =
            $"Paste is now '{paste.Label}' and the size box reads {size.Value:F0} — "
            + $"{renames} write{(renames == 1 ? string.Empty : "s")} to the same two objects";
    }

    private void OnAddTab(object sender, RoutedEventArgs arguments)
    {
        added++;

        var group = new RibbonGroup { Label = $"Group {added}", Priority = added };

        for (int i = 1; i <= 3; i++)
        {
            group.Items.Add(new RibbonButton
            {
                Label = $"Command {i}",
                IconSource = new SymbolIconSource { Symbol = Symbol.Play },
            });
        }

        var tab = new RibbonTab { Label = $"Made at run time {added}" };
        tab.Groups.Add(group);

        Ribbon.Tabs.Add(tab);
        Ribbon.SelectedIndex = Ribbon.Tabs.Count - 1;

        AddTabResult.Text = $"{Ribbon.Tabs.Count} tabs, the last {added} of them built here";
    }
}
