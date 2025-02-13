# OpenTelemetry.AutoInstrumentation.Digma

### How To Install:
1. Copy the dll the to app's directory (where the exe or the rest of the dependencies are located).
2. Add this environment variable and value to the process:
   ```
   OTEL_DOTNET_AUTO_PLUGINS=OpenTelemetry.AutoInstrumentation.Digma.Plugin, OpenTelemetry.AutoInstrumentation.Digma, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
   ```

