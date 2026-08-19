# Contributing

Thanks for taking an interest in Digi21.WinUI.Ribbon. Issues and pull requests are welcome.

The library is still before its first release, so the public API can still change. If you are
about to build something sizeable on top of it, or your change touches the public API, please
open an issue first: it is cheaper to agree on the shape of an API than to redo a pull request.

## Building and running

You need Windows, the .NET 8 SDK or later, and the Windows App SDK 1.8 packages (they restore from
NuGet; there is no workload to install). Visual Studio is optional; everything below works from the
command line.

```
dotnet build
dotnet test
dotnet run --project samples/RibbonGallery
```

The repository holds three projects:

- `src/Digi21.WinUI.Ribbon` — the library, and the only thing that ships.
- `samples/RibbonGallery` — an unpackaged WinUI app showing what the control can do. It is a
  demonstration for someone deciding whether to use the library, not a test bench.
- `tests/Digi21.WinUI.Ribbon.Tests` — xUnit tests.

The library depends on nothing but the Windows App SDK, and it is meant to stay that way.

## Reporting bugs

Most ribbon bugs are about a width or a sequence of them, so say how wide the window was and what
you did to it ("narrow it until Clipboard collapses, widen it back, switch tabs"). Say which items
were in the group and which size variants they declared, because that is what the layout decides
from. Please include the Windows build, the Windows App SDK version, and whether the app is packaged
or unpackaged. If you can reproduce it in `samples/RibbonGallery`, say how.

## Tests

The tests cover what does not need a XAML runtime, and the layout algorithm is deliberately in that
set: given an available width and a set of groups with their priorities and the size variants each
item accepts, which size every item takes and which groups end up collapsed is plain logic over
plain objects, and it is tested as such. **Keep it out of the controls.** A layout rule that can only
be exercised by measuring a live element is a rule nobody can write a test for.

Everything that needs a live visual tree — that there is one instance of an item and never two, that
the focus reaches a hosted `TextBox`, that the automation tree is complete, that icons stay visible
across a theme change, that a relayout is fast enough to drag a window border by — is measured in
**RibbonProbe**, a separate private harness. It is not in this repository on purpose: it is
maintainer's scaffolding, not a sample. It references this repository by source, so it sits next
door.

If you change how items are laid out or realized, run it and put the numbers in the pull request.
And when you add a measurement there to catch a specific bug, take the fix out and check that the
probe reports it — a harness that always says zero is not evidence of anything.

## Code style

`.editorconfig` carries the formatting rules, and the build treats warnings as errors, including
missing XML documentation on public members. Beyond that:

- Public types and members need XML documentation that says what they are for, not what they are
  called. Everything else uses plain `//` comments: the compiler writes a `<member>` entry for
  every `///` comment whatever its accessibility, so a `///` on an internal member ships in the
  package's `.xml` and reads as API that does not exist.
- Comments explain *why*. A lot of this code is shaped by constraints that look arbitrary until you
  know them — a WinUI quirk, an invariant of the layout — and those are worth a sentence. Comments
  restating the code are not.
- **An item is realized once.** Every arrangement of the ribbon reuses the element that already
  exists rather than building another from a template. This is the reason the library was written,
  so a change that reintroduces cloning is a bug however convenient it looks.
- Every value a template shows comes from a `{ThemeResource Ribbon*}` key, never a hard-coded colour
  or size. That is what lets an application recolour the ribbon without retemplating it.
- Match the surrounding code: file-scoped namespaces, nullable enabled, explicit types over `var`,
  no abbreviations in names.

## Versioning

There is no version number written down anywhere. MinVer works it out from the git history at build
time: a commit tagged `v1.2.3` builds as `1.2.3`, and an untagged one builds as a pre-release of the
version being worked towards — `0.1.0-dev.7`, where the number is how many commits there have been
since the last tag.

Two things follow from that. `dotnet pack` on any commit produces a version nobody has used before,
so a local feed can hold a run of them and a consumer picks up a new one without anybody clearing
the NuGet cache. And releasing is one command:

```
git tag v0.1.0 && git push origin v0.1.0
```

Pushing a `v*` tag is what triggers the release workflow, which builds, tests, packs and publishes
to nuget.org. Nothing else does, so an ordinary push is always safe.

## Commits and pull requests

Commit messages are in English and follow the conventional style used in the history
(`feat:`, `fix:`, `chore:`, `docs:`, `test:`), with a body explaining the reasoning when the subject
is not self-explanatory. Keep one topic per pull request; unrelated cleanups are easier to review on
their own.

When your change affects the public API or the behaviour a user can see, update `README.md` and add
an entry to `CHANGELOG.md` under `Unreleased`.

CI builds the solution, runs the tests and packs the library on every pull request; it has to pass.

## License

By contributing you agree that your contributions are licensed under the
[MIT License](LICENSE), like the rest of the project.
