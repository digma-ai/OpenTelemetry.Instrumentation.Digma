[![NuGet Status](https://img.shields.io/nuget/v/OpenTel.Instrumentation.Digma.svg?style=plastic)](https://www.nuget.org/packages/OpenTel.Instrumentation.Digma/)

# OpenTelemetry-Instrumentation-Digma

This nuget package contains additional OTEL attributes instrumentation.
These additiona attributes provide context on code locations which then make it possible to map insights derived from the observability, back to source code objects.

To read more about why you would do that, check out the [Digma](https://github.com/digma-ai/digma) repo.

### Using the code

Here is a simple example of including the Digma instrumentation in the OTEL tracing configuration:

```csharp
builder.Services.AddEndpointMonitoring();
builder.Services.AddOpenTelemetryTracing(
    builder => builder
        .AddAspNetCoreInstrumentation(options =>{options.RecordException = true;})
        .AddHttpClientInstrumentation()
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddTelemetrySdk()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion ?? "0.0.0")
            .AddDigmaAttributes())
        .AddOtlpExporter(c => { c.Endpoint = new Uri(collectorUrl);})
        .AddSource("*")
); 
```

### DigmaConfigurationOptions 

While the instrumentation will try to automatically collect relevant information based on convention, it is possible to explicitly set the data and source.

| Option | Description | Default Value | Mandatory |
| ------ | ----------- | ------------- |-----------|
| ```NamespaceRoot``` | The root namespace for this project. Anything under this namespace will be traced. | Digma will try to infer that from the current class. | False |
| ```Environment``` | The Environment describes where the running process is deployed. (e.g production, staging, ci)  | Ths instrumentation will attempt to read it from an environment variable. Empty value is 'UNSET' | False |
| ```EnvironmentEnvVariable``` | The environment variable name that will be used to check the Environment of the running process to tag the observability data | 'DEPLOYMENT_ENVIORNMENT'| False|
| ```CommitId``` | The specific commit identifier of the running code. | The instrumentation will attempt to read this variable from an environment variable, which can be injected by the CI | False |
| ```CommitIdEnvVariable``` | The environment variable used to store the commit identifer | 'DEPLOYMENT_COMMIT_ID' | False |

### Usage Example

To see a working end-to-end example, check out our DotNet [sample application repo](https://github.com/digma-ai/otel-sample-application-dotnet). 
