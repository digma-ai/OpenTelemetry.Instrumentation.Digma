[![NuGet Status](https://img.shields.io/nuget/v/OpenTel.Instrumentation.Digma.svg?style=plastic)](https://www.nuget.org/packages/OpenTel.Instrumentation.Digma/)
[![CI](https://github.com/digma-ai/OpenTelemetry.Instrumentation.Digma/actions/workflows/dotnet.yml/badge.svg)](https://github.com/digma-ai/OpenTelemetry.Instrumentation.Digma/actions/workflows/dotnet.yml)

# Release Guidelines

### OpenTelemetry.Instrumentation.Digma:

To release OpenTelemetry.Instrumentation.Digma project as nuget, 
A GitHub workflow need to be triggered when a tag with prefix
"OpenTelemetry.Instrumentation.Digma-%Version%" is pushed. 

```
git tag OpenTelemetry.Instrumentation.Digma-%VERSION%
git push origin OpenTelemetry.Instrumentation.Digma-%VERSION%
```

### Digma.MassTransit.Integration:

To release Digma.MassTransit.Integration project as nuget,
A GitHub workflow need to be triggered when a tag with prefix
"Digma.MassTransit.Integration-%Version%" is pushed.

```
git tag Digma.MassTransit.Integration-%VERSION%
git push origin Digma.MassTransit.Integration-%VERSION%
```
