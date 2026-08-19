using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Keeps docs/localisation.md and RibbonStrings from drifting apart.
//
// The guide is a file nothing compiles, so a property added to the class arrives untranslated in
// nine languages and nobody notices until somebody who cannot see the screen is read a blank. These
// tests are what make that a red build instead.
//
// The guide is copied beside the test assembly by the test project, which is how it is found here.
public class LocalisationTests
{
    private static readonly string[] Languages = ["ca", "en", "eu", "fr", "gl", "de", "it", "pt", "es"];

    private static readonly string Guide =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "localisation.md"));

    public static TheoryData<string> EveryLanguage => [.. Languages];

    [Fact]
    public void EveryStringOfTheClassIsInTheGuide()
    {
        foreach (string name in Strings())
        {
            Assert.Contains($"RibbonStrings.{name} =", Guide, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheGuideInventsNoStringTheClassDoesNotHave()
    {
        var known = Strings().ToHashSet(StringComparer.Ordinal);

        foreach (Match mentioned in Regex.Matches(Guide, @"RibbonStrings\.(\w+) ="))
        {
            Assert.Contains(mentioned.Groups[1].Value, known);
        }
    }

    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void EveryLanguageSetsEveryString(string language)
    {
        string section = Section(language);

        foreach (string name in Strings())
        {
            Assert.True(
                section.Contains($"RibbonStrings.{name} =", StringComparison.Ordinal),
                $"'{language}' does not set {name}");
        }
    }

    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void EveryTranslationKeepsThePlaceholderItsSentenceNeeds(string language)
    {
        // A translation that drops the {0} produces a launcher every screen reader calls the same
        // thing, which is the failure the sentence exists to avoid, and it looks perfectly fine in
        // a review.
        string section = Section(language);

        foreach (string name in Strings().Where(name => name.EndsWith("Format", StringComparison.Ordinal)))
        {
            Match assignment = Regex.Match(section, $@"RibbonStrings\.{name} = ""([^""]*)"";");

            Assert.True(assignment.Success, $"'{language}' does not set {name}");
            Assert.True(assignment.Groups[1].Value.Contains("{0}", StringComparison.Ordinal), $"'{language}' drops the placeholder from {name}");
        }
    }

    [Fact]
    public void TheClassStartsInEnglish()
    {
        // The defaults are what an application that never sets any of these shows, so they have to
        // be the English of the guide rather than a placeholder nobody translated.
        string english = Section("en");

        foreach (PropertyInfo property in Properties())
        {
            string value = (string)property.GetValue(null)!;

            Assert.True(
                english.Contains($@"RibbonStrings.{property.Name} = ""{value}"";", StringComparison.Ordinal),
                $"the default of {property.Name} is \"{value}\", which the English section does not say");
        }
    }

    private static IEnumerable<string> Strings() => Properties().Select(property => property.Name);

    private static PropertyInfo[] Properties() =>
        typeof(RibbonStrings).GetProperties(BindingFlags.Public | BindingFlags.Static);

    // Everything under one language's heading, up to the next heading.
    private static string Section(string language)
    {
        Match heading = Regex.Match(Guide, $@"^## .+ \(`{language}`\)$", RegexOptions.Multiline);
        Assert.True(heading.Success, $"the guide has no section for '{language}'");

        Match next = Regex.Match(Guide[(heading.Index + heading.Length)..], "^## ", RegexOptions.Multiline);

        return next.Success
            ? Guide.Substring(heading.Index, heading.Length + next.Index)
            : Guide[heading.Index..];
    }
}
