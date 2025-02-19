# OpenTelemetry.AutoInstrumentation.Digma
For now, this dll only fix the behaviour of `System.Data.SqlClient` to always expose the sql statement for otel

### Build:
Builds the artifacts for all the specified frameworks. If the desired framework is not there, edit the csproj file and
add it to the `<TargetFrameworks>` section.
```
dotnet pack -c Release
```

### Install:
1. Copy the dlls from the desired framework `src\OpenTelemetry.AutoInstrumentation.Digma\bin\Release\<framework>` the to app's directory (where the exe or the rest of the dependencies are located).
 
2. Add this environment variable and value to the process:
   ```
   OTEL_DOTNET_AUTO_PLUGINS=OpenTelemetry.AutoInstrumentation.Digma.Plugin, OpenTelemetry.AutoInstrumentation.Digma, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
   OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT=true
   OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES=*
   ```
3. To activate the Extended Observability add `OTEL_DOTNET_AUTO_NAMESPACES` with a comma seperated list of namespaces,
or classes (full name), you want their methods to be dynamically wrapped with `Activity`.
4. To apply Extended Observability not only on public methods, but on private ones as well set
   ```
   OTEL_DOTNET_AUTO_INCLUDE_PRIVATE_METHODS=true
   ```
   
### Troubleshooting
Logs location (default: `%ProgramData%\OpenTelemetry .NET AutoInstrumentation\logs`):
```
OTEL_DOTNET_AUTO_LOG_DIRECTORY=...
```
Logs verbosity can be either `none`, `debug`, `info`, `warn`, or `error` (default: `info`):
```
OTEL_LOG_LEVEL=...
```
