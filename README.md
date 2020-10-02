# Nullean.VsTest.Pretty.TestLogger


Install from [nuget.org](https://www.nuget.org/packages/Nullean.VsTest.Pretty.TestLogger/)

```
$ dotnet add package Nullean.VsTest.Pretty.TestLogger
```

This logger is intended to replace the default console logger.
To do so create a `.runsettings` file 

```xml
<RunSettings>
  <LoggerRunSettings>
    <Loggers>
        <Logger friendlyName="Console" enabled="False"/>
        <Logger friendlyName="pretty" enabled="True"/>
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```

Then make your `Test.csproj` (or `fsproj`!) aware of these `.runsettings`.

```xml
<PropertyGroup>
  <RunSettingsFilePath>$(MSBuildProjectDirectory)\.runsettings</RunSettingsFilePath>
</PropertyGroup>
```

### Key goals

* Clear feedback during tests
* Notify of slow tests (taking longer then 2 seconds)
* Pretty print stacktraces beyond a see of red
* Grep for failures using `[FAIL]`
* Make sure all failures are printed at the end of the run.

### Show me the goods

This is what running tests will look like once configured:

[![asciicast](https://asciinema.org/a/363126.svg)](https://asciinema.org/a/363126?autoplay=1)

For comparison this is what `dotnet test` looks like out of the box **without** this logger

[![asciicast](https://asciinema.org/a/363123.svg)](https://asciinema.org/a/363123?autoplay=1)




