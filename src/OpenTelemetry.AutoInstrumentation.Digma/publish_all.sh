#!/bin/bash

frameworks=$(sed -n 's|.*<TargetFrameworks>\(.*\)</TargetFrameworks>.*|\1|p' OpenTelemetry.AutoInstrumentation.Digma.csproj)
IFS=';' read -ra targets <<< "$frameworks"

for tfm in "${targets[@]}"; do
    echo "Publishing for $tfm..."
    dotnet publish -c Release -f $tfm -o "bin/Publish/$tfm"
done

 zip -r bin/Publish/OpenTelemetry.AutoInstrumentation.Digma.zip bin/Publish