---
name: vstest-pretty-logger
description: >-
  Use when a .NET test project's `dotnet test` output should be terse and readable: only failures, skips and slow
  tests, then every failure again with its stack trace and a summary. Nullean.VsTest.Pretty.TestLogger is a VSTest
  logger (friendly name `pretty`) that replaces the console logger.
---

# Nullean.VsTest.Pretty.TestLogger

A VSTest logger, so it works with `dotnet test` on VSTest (xUnit v2, NUnit, MSTest with `Microsoft.NET.Test.Sdk`). It
does not work on Microsoft.Testing.Platform, which has no VSTest loggers.

## Turn it on for a test project

```xml
<!-- the test .csproj: a build-time dependency, never a runtime one -->
<ItemGroup>
  <PackageReference Include="Nullean.VsTest.Pretty.TestLogger" PrivateAssets="All" />
</ItemGroup>
<PropertyGroup>
  <RunSettingsFilePath>$(MSBuildProjectDirectory)\.runsettings</RunSettingsFilePath>
</PropertyGroup>
```

```xml
<!-- .runsettings next to it -->
<RunSettings>
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName="Console" enabled="False" />
      <Logger friendlyName="pretty" enabled="True" />
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```

For one run instead: `dotnet test --logger pretty`. Other loggers can run next to it, for example
`--logger GitHubActions` from the GitHubActionsTestLogger package.

## Hide skipped tests by namespace

```xml
<!-- inside <RunSettings> -->
<TestRunParameters>
  <Parameter name="DisableFullSkipMessages" value="MyTests.Examples.;MyTests.Integration." />
</TestRunParameters>
```

Skipped tests whose fully qualified name starts with one of the `;`-separated prefixes print nothing; they still
count in the summary.

## Gotchas

- Disable the `Console` logger when `pretty` is on, or both loggers write their own output for the same run.
- Fast passing tests print no line at all; a run with only passes shows just the summary.
- Slow means longer than two seconds. The threshold is fixed.
- Grep for `[FAIL]` to find the failures repeated at the end of the run.
