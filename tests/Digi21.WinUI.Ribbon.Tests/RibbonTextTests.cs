using System.Text.RegularExpressions;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Keeps the sentences the ribbon says on its own account and the dictionary that carries their
// defaults from coming apart.
//
// Themes/RibbonResources.xaml is the default an application overrides, and RibbonText carries the
// same English for the case where there is no application to have merged that dictionary - the XAML
// designer, and these tests. Two spellings of one sentence is two things to keep right, so this is
// what makes the second one a red build rather than a difference nobody sees until a designer shows
// a word the running application does not.
//
// The dictionary is copied beside the test assembly by the test project, which is how it is found
// here.
public class RibbonTextTests
{
    // Read with the line endings taken out, for the same reason the translation guide is: a fresh
    // checkout on Windows turns them into CRLF and a working tree that has never been through one
    // keeps whatever it was written with.
    private static readonly string Dictionary =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "RibbonResources.xaml")).Replace("\r\n", "\n", StringComparison.Ordinal);

    public static TheoryData<string> EveryKey => [.. RibbonText.Keys];

    [Theory]
    [MemberData(nameof(EveryKey))]
    public void TheDictionaryDeclaresEveryKeyTheRibbonReads(string key)
    {
        Assert.True(Declared().ContainsKey(key), $"Themes/RibbonResources.xaml declares no '{key}'");
    }

    [Fact]
    public void TheDictionaryDeclaresNoSentenceTheRibbonNeverReads()
    {
        // A key left behind by a rename is a default nothing looks at, and it looks exactly like one
        // that is still in use.
        foreach (string declared in Declared().Keys)
        {
            Assert.Contains(declared, RibbonText.Keys);
        }
    }

    [Theory]
    [MemberData(nameof(EveryKey))]
    public void TheEnglishInTheDictionaryIsTheEnglishInTheCode(string key)
    {
        Assert.Equal(RibbonText.Fallback(key), Declared()[key]);
    }

    [Theory]
    [MemberData(nameof(EveryKey))]
    public void NoDefaultCarriesTheEscapeWpfWouldWant(string key)
    {
        // A sentence that starts with a placeholder is written as it stands. WPF reads an opening
        // brace in a value as the beginning of a markup extension and wants {} in front of one;
        // WinUI reads the content of an x:String literally, and the escape it does not know is a
        // parse error that takes the application down before its first window. This is a red build
        // rather than an afternoon.
        Assert.False(
            Declared()[key].StartsWith("{}", StringComparison.Ordinal),
            $"the default of '{key}' starts with the escape WPF wants, which WinUI cannot parse");
    }

    [Fact]
    public void TheKeysAllCarryThePrefixTheRestOfTheDictionaryUses()
    {
        // One prefix for every key this library reads out of the application's resources, so that
        // what belongs to the ribbon can be told from what belongs to anything else in there.
        foreach (string key in RibbonText.Keys)
        {
            Assert.StartsWith("Ribbon", key, StringComparison.Ordinal);
        }
    }

    private static Dictionary<string, string> Declared() =>
        Regex.Matches(Dictionary, @"<x:String x:Key=""(\w+)"">([^<]*)</x:String>")
            .ToDictionary(entry => entry.Groups[1].Value, entry => entry.Groups[2].Value, StringComparer.Ordinal);
}
