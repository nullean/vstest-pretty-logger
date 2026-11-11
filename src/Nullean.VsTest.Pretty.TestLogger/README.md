# Nullean.VsTest.Pretty.TestLogger

A VSTest logger for `dotnet test` that replaces the default console output. It prints a coloured line for every failed,
skipped or slow test (over two seconds) and leaves fast passing tests out, shows readable stack traces, and at the end
repeats every failure, followed by a summary of passed, failed, slow and skipped tests.

Reference it from the test project:

```xml
<PackageReference Include="Nullean.VsTest.Pretty.TestLogger" Version="*" PrivateAssets="All" />
```

Then turn the console logger off and this one on in a `.runsettings` file the project points at:

```xml
<RunSettings>
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName="Console" enabled="False" />
      <Logger friendlyName="pretty" enabled="True" />
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```

```xml
<PropertyGroup>
  <RunSettingsFilePath>$(MSBuildProjectDirectory)\.runsettings</RunSettingsFilePath>
</PropertyGroup>
```

Or pick it for a single run: `dotnet test --logger pretty`.

To hide the skipped tests of some namespaces, list their prefixes in a run parameter:

```xml
<TestRunParameters>
  <Parameter name="DisableFullSkipMessages" value="MyTests.Examples.;MyTests.Slow." />
</TestRunParameters>
```

The failures repeated at the end are marked `[FAIL]`, so they are easy to grep. See
https://github.com/nullean/vstest-pretty-logger.
