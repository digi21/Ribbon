using System.Text.RegularExpressions;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

// Keeps docs/localisation.md and the keys the ribbon reads from drifting apart.
//
// The guide is a file nothing compiles, so a sentence added to the library arrives untranslated in
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
    public void EveryKeyTheRibbonReadsIsInTheGuide()
    {
        foreach (string key in RibbonText.Keys)
        {
            Assert.Contains(Entry(key), Guide, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheGuideInventsNoKeyTheRibbonDoesNotRead()
    {
        // A key the guide has and the library does not is nine translations of a sentence nobody
        // will ever hear, and it reads exactly like the eight beside it.
        foreach (Match mentioned in Regex.Matches(Guide, @"<x:String x:Key=""(\w+)"">"))
        {
            Assert.Contains(mentioned.Groups[1].Value, RibbonText.Keys);
        }
    }

    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void EveryLanguageSetsEveryKey(string language)
    {
        string section = Section(language);

        foreach (string key in RibbonText.Keys)
        {
            Assert.True(
                section.Contains(Entry(key), StringComparison.Ordinal),
                $"'{language}' does not set {key}");
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

        foreach (string key in RibbonText.Keys.Where(key => key.EndsWith("Format", StringComparison.Ordinal)))
        {
            Match declaration = Regex.Match(section, $@"{Entry(key)}([^<]*)</x:String>");

            Assert.True(declaration.Success, $"'{language}' does not set {key}");

            // Which placeholders a sentence needs is read off the sentence itself rather than
            // assumed to be one: what a contextual tab in a group announces takes the tab's name and
            // the heading's, and a translation that keeps the first and drops the second reads
            // perfectly well and leaves out the half that groups it.
            foreach (Match placeholder in Regex.Matches(RibbonText.Fallback(key), @"\{\d+\}"))
            {
                Assert.True(
                    declaration.Groups[1].Value.Contains(placeholder.Value, StringComparison.Ordinal),
                    $"'{language}' drops {placeholder.Value} from {key}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void NoTranslationCarriesTheEscapeWpfWouldWant(string language)
    {
        // The one way a translation of these can fail before anybody hears it: WPF reads an opening
        // brace in a value as the beginning of a markup extension and wants {} in front of one, and
        // a translator who knows that writes it. WinUI reads the content of an x:String literally,
        // and the escape it does not know takes the application down before its first window.
        string section = Section(language);

        foreach (Match declaration in Regex.Matches(section, @"<x:String x:Key=""\w+"">([^<]*)</x:String>"))
        {
            Assert.False(
                declaration.Groups[1].Value.StartsWith("{}", StringComparison.Ordinal),
                $"'{language}' writes the escape WinUI cannot parse: '{declaration.Groups[1].Value}'");
        }
    }

    [Fact]
    public void TheLibraryStartsInEnglish()
    {
        // The defaults are what an application that declares none of these shows, so they have to be
        // the English of the guide rather than a placeholder nobody translated.
        string english = Section("en");

        foreach (string key in RibbonText.Keys)
        {
            string value = RibbonText.Fallback(key);

            Assert.True(
                english.Contains($"{Entry(key)}{value}</x:String>", StringComparison.Ordinal),
                $"the default of {key} is \"{value}\", which the English section does not say");
        }
    }

    private static string Entry(string key) => $@"<x:String x:Key=""{key}"">";

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
