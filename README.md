[![NuGet Status](https://img.shields.io/nuget/v/OpenTel.Instrumentation.Digma.svg?style=plastic)](https://www.nuget.org/packages/OpenTel.Instrumentation.Digma/)

# OpenTelemetry-Instrumentation-Digma

This nuget package contains several instrumentation helpers for OpenTelemetry.

[Digram Instrumentation helpers](#digma_instrumentation) 

[TracingDecorator](#tracingdecorator) 


<a name="digma_instrumentation"/>

## Digram Instrumentation helpers
The instrumentation helpers allow injecting spans with an aditional set of span attributes which provide context on code locations which then make it possible to map insights derived from the observability, back to source code objects.

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

<a name="tracingdecorator"/>

## Tracing Decorator

The TracingDecorator class allows automatically instrumenting a given interface's methods. 
Interceptors have their pros and cons, please avoid using this class with performance sentitive short lived operations.

### Setup:
Create an instance of the decorator. The static constructor accepts as a parameter the concrete object you want to decorate:
```csharp
decoratedProxy = TraceDecorator<IInterfaceDecorated>.Create(actualIntance)
```
You can easily inject your decorator into the DI by using the [Scrutor](https://www.nuget.org/packages/Scrutor/) package.
For example:
```csharp
builder.Services.Decorate<IInterfaceDecorated>((decorated) =>
    TraceDecorator<IInterfaceDecorated>.Create(decorated));
```

#### TraceDecorator Create parameters:
| Parameter | Type |Description | Default Value | Mandatory |
| ------ | ----------- | ------------- |-----------|--------|
| ```decorated``` | TDecorated | The object to decorate with auto-instrumentation behavior | null | True |
| ```activityNamingSchema``` | IActivityNamingSchema? | The naming schema that will be used to automatically name spans. You can add your own naming schemas based on convention | ```MethodFullNameSchema``` | False |
| ```decorateAllMethods``` | bool | Whether to automatically decorate all of the interface operations | ```true``` | False |

