# vstest-pretty-logger

`Nullean.VsTest.Pretty.TestLogger`: a VSTest logger (friendly name `pretty`) that replaces the default console
logger for `dotnet test`. It prints each result with colours and markers, flags tests slower than two seconds,
pretty-prints stack traces and repeats every failure at the end of the run.

## Layout

| Path | What |
|---|---|
| `src/Nullean.VsTest.Pretty.TestLogger` | the logger (netstandard2.0); `PrettyLogger.cs` is the VSTest entry point, `TestConsoleWriter.cs` the output |
| `tests/*` | example test projects (net11.0) wired to the logger through `.runsettings`; their tests fail, skip and run slow **on purpose** to show its output |
| `build/scripts` | the F# build ([Nullean.Make.Fs](https://www.nuget.org/packages/Nullean.Make.Fs), net11.0) the `mise` tasks call |
| `build/keys` | the strong-name key the logger is signed with |

## Commands

mise installs the SDK pinned in `global.json`. Run everything through it:

```bash
mise run build    # build the solution
mise run pack     # build, pack, validate the package, report public API changes
mise run ci       # exactly what CI runs
mise run format   # format the repository with curb
mise run check    # formatting drift (curb) and workflow security checks (zizmor); CI runs the curb half
mise run clean    # delete build output
mise run release  # show the release draft; `mise run release publish` releases
```

`mise tasks ls` lists them all. The package and API reports go to `build/output`.

There is no `test` task: the build's `test` target is deliberately a no-op, because the example tests are meant to
fail. To see the logger at work, run one of them, for example
`dotnet test tests/Nullean.PrettyLogger.Tests -c Release`, and expect a failing exit code.

## Releasing

release-drafter keeps a draft release up to date from the labels of merged pull requests. **Publishing the draft**
releases: it creates the tag, and the `release` job in `ci.yml` packs at that version and pushes to nuget.org.
`mise run release` shows the draft. The [releasing](.claude/skills/releasing/SKILL.md) and
[breaking-changes](.claude/skills/breaking-changes/SKILL.md) skills have the details. Every pull request carries exactly
one release label.

## Packaging

The package ships its README (`src/Nullean.VsTest.Pretty.TestLogger/README.md`) and an agent skill
(`src/Nullean.VsTest.Pretty.TestLogger/skills/vstest-pretty-logger/SKILL.md`), packed at `skills/`. Keep both in step
with the logger: the friendly name, the `.runsettings` wiring and `DisableFullSkipMessages`.

## Rules

- The friendly name `pretty`, the extension URI and the `DisableFullSkipMessages` run parameter are what users'
  `.runsettings` files name. Changing them is a breaking change.
- The logger targets netstandard2.0 and references the latest `Microsoft.TestPlatform.ObjectModel`, like every other
  dependency. Its floor is the oldest VSTest that can load the logger, so a major bump of it is a breaking change.
- Build targets are Make.Fs `target { }` blocks in `build/scripts/Program.fs` whose commands are Proc.Fs `sh $"…"`
  lines; values come from `text` inside `shell { }` (`Versions.fs`). Do not call the C# `ProcNet` API from the build.
