# OpenTelemetry.AutoInstrumentation.Digma
For now, this dll only fix the behaviour of `System.Data.SqlClient` to always expose the sql statement for otel

### Build
Builds the artifacts for all the specified frameworks. If the desired framework is not there, edit the csproj file and
add it to the `<TargetFrameworks>` section.
```
./publish_all.sh
```

### Install
1. Copy the dlls from the desired framework `src\OpenTelemetry.AutoInstrumentation.Digma\bin\Release\<framework>` the to app's directory (where the exe or the rest of the dependencies are located).
 
2. Add this environment variable and value to the process:
   ```
   OTEL_DOTNET_AUTO_PLUGINS=OpenTelemetry.AutoInstrumentation.Digma.Plugin, OpenTelemetry.AutoInstrumentation.Digma, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
   OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT=true
   OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES=*
   ```
3. Add a JSON file with automoinstrumentation rules. The Digma automatic observability uses a JSON rules file in order to specify which code to include. By default, the Digma OTEL plugin will look for a file called `autoinstrumentation.rules.json` in the application directory. You can specify a different file path by setting the `DIGMA_AUTOINST_RULES_FILE` environment variable.

### Automatic instrumentation rules

This JSON file defines include and exclude rules to control which code elements are automatically instrumented for tracing in your application.

#### Structure Overview

```json
{
  "include": [ ... ], // List of rules specifying which code elements should be instrumented.
  "exclude": [ ... ]  // List of rules specifying which code elements should be excluded, even if they match an include rule.
}
```

#### Include/Exclude Rule Format

Each object in the include array supports the following optional fields:
| Field | Type | Description |
| -------- | ------- | ------- |
| namespaces | string | Namespace pattern to match. Wildcards (*) and regex (/^.../) supported. |
| classes | string |  Class name pattern. Wildcards or regex (/^.../) are supported. |
| methods | string | Method name pattern. Wildcards or regex (/^.../) are supported. |
| syncModifier | string  | Restrict the rule to specific types of methods - "Async" or "Sync" |
| accessModifier | string | "Public" or "Private" to match method visibility. |
| nestedOnly | boolean | Relevant for include rules only. If true, this code will only be instrumented as a part of already instrumented endpoints and will not start a new trace

If multiple fields are specified, all must match for the rule to apply.

#### Wildcards vs Regex

| Pattern Type | Syntax | Example | Description | 
| -------- | ------- | ------- | ------- | 
| Wildcard | "Acme.*" | Matches any sub-namespace under Digma |
| Regex | "/^Acme\\.V\\d+/" | Matches namespaces like Acme.V1, Acme.V2, etc.

#### 🧪 Example

```json
{
    "include":[
        {
            "namespaces": "Digma.*"     // namespaces field is the minimum
        },
        {
            "namespaces": "Digma.Repositories.*",
            "classes": "Repo*",
            "methods": "Get*",
            "syncModifier": "Async",     // Async or Sync, default is null
            "accessModifier": "Public",  // Private or Public, default is null
            "nestedOnly": true           // default is false
        },
        {
            "namespaces": "/^Digma\\.V\\d+/",  // Regex
            "classes": "/^Repo\\w+/",          // Regex
            "methods": "/^Get\\w+/",           // Regex
        }
    ],
    "exclude":[
        {
            "namespaces": "Digma.Tools.*"
        }
    ]
}
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
