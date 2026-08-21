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

    // Read with the line endings taken out, because they are not the same everywhere the tests run.
    // A working tree that has never been through a checkout keeps whatever it was written with, and
    // a fresh checkout on Windows turns it into CRLF - and `$` in .NET matches before the newline,
    // which with CRLF is after the carriage return rather than after the last character of the line.
    // These tests passed here and failed on the first build server that saw them.
    private static readonly string Guide =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "localisation.md")).Replace("\r\n", "\n", StringComparison.Ordinal);

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

        foreach (PropertyInfo property in Properties().Where(property => property.Name.EndsWith("Format", StringComparison.Ordinal)))
        {
            Match assignment = Regex.Match(section, $@"RibbonStrings\.{property.Name} = ""([^""]*)"";");

            Assert.True(assignment.Success, $"'{language}' does not set {property.Name}");

            // Which placeholders a sentence needs is read off the sentence itself rather than
            // assumed to be one: what a contextual tab in a group announces takes the tab's name and
            // the heading's, and a translation that keeps the first and drops the second reads
            // perfectly well and leaves out the half that groups it.
            foreach (Match placeholder in Regex.Matches((string)property.GetValue(null)!, @"\{\d+\}"))
            {
                Assert.True(
                    assignment.Groups[1].Value.Contains(placeholder.Value, StringComparison.Ordinal),
                    $"'{language}' drops {placeholder.Value} from {property.Name}");
            }
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
