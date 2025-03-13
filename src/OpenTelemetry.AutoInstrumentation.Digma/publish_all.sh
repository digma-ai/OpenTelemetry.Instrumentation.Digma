#!/bin/bash

frameworks=$(sed -n 's|.*<TargetFrameworks>\(.*\)</TargetFrameworks>.*|\1|p' OpenTelemetry.AutoInstrumentation.Digma.csproj)
IFS=';' read -ra targets <<< "$frameworks"

for tfm in "${targets[@]}"; do
    echo "Publishing for $tfm..."
    dotnet publish -c Release -f $tfm -o "bin/Publish/$tfm"
done

cd bin/Publish/
zip -r OpenTelemetry.AutoInstrumentation.Digma.zip .
cd ../../