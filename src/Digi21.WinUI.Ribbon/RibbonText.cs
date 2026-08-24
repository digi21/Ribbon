using System.Globalization;
using Microsoft.UI.Xaml;

namespace Digi21.WinUI.Ribbon;

// Every sentence the ribbon says on its own account, read from the application's resources at the
// moment it is said.
//
// The keys are declared in Themes/RibbonResources.xaml, beside the brushes and the metrics, which is
// what makes one mechanism out of everything an application can change about this library: a colour,
// a number and a sentence are the same key redeclared in the same file. The alternative was a class
// of static properties, and a static is one variable for a whole process - one host with two
// plug-ins using this library has one set of sentences between them, and whichever wrote last wins.
//
// The English here is not the default an application overrides; the entry in that dictionary is.
// This is what answers when there is no application to have merged it, which is the XAML designer
// and a unit test. RibbonTextTests keeps the two in step, so the English cannot come apart from the
// English in the dictionary without a red build.
internal static class RibbonText
{
    internal const string GroupLauncherNameFormat = "RibbonGroupLauncherNameFormat";
    internal const string CollapsedGroupNameFormat = "RibbonCollapsedGroupNameFormat";
    internal const string ContextualTabNameFormat = "RibbonContextualTabNameFormat";
    internal const string ContextualTabInGroupNameFormat = "RibbonContextualTabInGroupNameFormat";
    internal const string MinimizeRibbonName = "RibbonMinimizeRibbonName";
    internal const string ExpandRibbonName = "RibbonExpandRibbonName";
    internal const string SimplifyRibbonName = "RibbonSimplifyRibbonName";
    internal const string FullRibbonName = "RibbonFullRibbonName";

    // The four that take the name of the thing they are about first, then the chevron's two pairs.
    // This is the order the theming table and the translation guide list them in, and the order an
    // application that walks Ribbon.TextResourceKeys sees them in.
    private static readonly (string Key, string English)[] Table =
    [
        (GroupLauncherNameFormat, "{0} options"),
        (CollapsedGroupNameFormat, "{0}, collapsed group"),
        (ContextualTabNameFormat, "{0}, contextual tab"),
        (ContextualTabInGroupNameFormat, "{0}, contextual tab, {1}"),
        (MinimizeRibbonName, "Minimise the ribbon"),
        (ExpandRibbonName, "Expand the ribbon"),
        (SimplifyRibbonName, "Simplify the ribbon"),
        (FullRibbonName, "Show the full ribbon"),
    ];

    private static readonly Dictionary<string, string> Fallbacks =
        Table.ToDictionary(entry => entry.Key, entry => entry.English, StringComparer.Ordinal);

    internal static IReadOnlyList<string> Keys { get; } = [.. Table.Select(entry => entry.Key)];

    // The English of a key nobody has declared, which is what answers when there is no application
    // to have merged the dictionary that carries the defaults. Read by the tests that keep the two
    // spellings of it in step.
    internal static string Fallback(string key) => Fallbacks[key];

    // Read here rather than kept in a field: an application may redeclare one of these after a
    // ribbon exists, and what a control announces itself as is composed each time the thing it names
    // changes - a group folding, the chevron turning over, a contextual tab arriving.
    internal static string Get(string key) =>
        Application.Current is { } application && TryFind(application.Resources, key, out string found)
            ? found
            : Fallbacks[key];

    internal static string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    // XAML's own lookup order, walked here rather than left to the dictionary to do: an entry the
    // application declared directly is found before any merged dictionary is looked in, and the
    // merged ones are looked in from the last to the first - which is what leaves the library's,
    // merged at the bottom by RibbonThemeResources, answering only when nobody else does.
    //
    // Walking it is what makes the guarantee hold whichever arrives first. A ribbon built before the
    // application declares its sentences and one built after it read the same dictionary the same
    // way, because the reading happens when the sentence is said and not when the control is made.
    private static bool TryFind(ResourceDictionary dictionary, string key, out string text)
    {
        if (dictionary.TryGetValue(key, out object? value) && value is string own)
        {
            text = own;
            return true;
        }

        for (int index = dictionary.MergedDictionaries.Count - 1; index >= 0; index--)
        {
            if (TryFind(dictionary.MergedDictionaries[index], key, out text))
            {
                return true;
            }
        }

        text = string.Empty;
        return false;
    }
}
