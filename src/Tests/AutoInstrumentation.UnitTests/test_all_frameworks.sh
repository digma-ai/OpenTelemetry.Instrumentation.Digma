#!/bin/bash

frameworks=$(sed -n 's|.*<TargetFrameworks>\(.*\)</TargetFrameworks>.*|\1|p' OpenTelemetry.AutoInstrumentation.Digma.Tests.csproj)
IFS=';' read -ra targets <<< "$frameworks"

for tfm in "${targets[@]}"; do
    echo "Testing for $tfm..."
    dotnet test -f $tfm
done