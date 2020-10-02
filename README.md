# Nullean.VsTest.Pretty.TestLogger


Install from [nuget.org](https://www.nuget.org/packages/Nullean.VsTest.Pretty.TestLogger/)

```
$ dotnet add package Nullean.VsTest.Pretty.TestLogger
```

This logger is intended to replace the default console logger.
To do so create a `.runsettings` file and make your `Test.csproj` aware of these `.runsettings`.

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

### Key goals

* Clear feedback during tests
* Notify of slow tests (taking longer then 2 seconds)
* Pretty print stacktraces beyond a see of red
* Grep for failures using `[FAIL]`
* Make sure all failures are printed at the end of the run.

### Show me the goods

This is what `dotnet test` looks like out of the box **without** this logger


[![asciicast](https://asciinema.org/a/363123.svg)](https://asciinema.org/a/363123)

And the update version

[![asciicast](https://asciinema.org/a/363126.svg)](https://asciinema.org/a/363126)



