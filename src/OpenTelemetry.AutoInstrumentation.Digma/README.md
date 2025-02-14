# OpenTelemetry.AutoInstrumentation.Digma
For now, this dll only fix the behaviour of `System.Data.SqlClient` to always expose the sql statement for otel

### Build:
Builds the artifacts for all the specified frameworks. If the desired framework is not there, edit the csproj file and
add it to the `<TargetFrameworks>` section.
```
dotnet pack -c Release
```

### Install:
1. Copy the dlls the to app's directory (where the exe or the rest of the dependencies are located).
   - `0Harmony.dll`
   - `OpenTelemetry.AutoInstrumentation.Digma.dll`
 
2. Add this environment variable and value to the process:
   ```
   OTEL_DOTNET_AUTO_PLUGINS=OpenTelemetry.AutoInstrumentation.Digma.Plugin, OpenTelemetry.AutoInstrumentation.Digma, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
   OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT=true
   ```
3. To activate the Extended Observability add `OTEL_DOTNET_AUTO_NAMESPACES` with a comma seperated list of namespaces,
or classes (full name), you want their methods to be dynamically wrapped with `Activity`.