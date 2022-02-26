// <copyright file="DigmaInstrumentationHelperExtensions.cs" company="Digma Team">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
namespace OpenTelemetry.Instrumentation.Digma;

using System.Diagnostics;
using OpenTelemetry.Resources;

public static class DigmaInstrumentationHelperExtensions
{
    public static ResourceBuilder AddDigmaAttributes(this ResourceBuilder builder,
                                                     Action<DigmaConfigurationOptions> configure = null)
    {
        var workingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        DigmaConfigurationOptions options = new DigmaConfigurationOptions();
        configure(options);

        //If namespace not provided try to get it from the calling method
        if (options.NamespaceRoot == null) { 

            StackTrace stackTrace = new StackTrace();
            options.NamespaceRoot = stackTrace?.GetFrame(1)?.GetMethod()?.DeclaringType?.Namespace ?? "";
        }

        if (options.Environment == null)
        {
            options.Environment = Environment.GetEnvironmentVariable(options.CommitIdEnvVariable) ?? "";
        }

        if (options.Environment == null)
        {
            options.Environment = Environment.GetEnvironmentVariable(options.EnvironmentEnvVariable) ?? "UNSET_ENV";
        }



        builder.AddAttributes(new[] {   new KeyValuePair<string, object>("deployment.environment", "Dev"),
                                        new KeyValuePair<string, object>("paths.working_directory", workingDirectory),
                                        new KeyValuePair<string, object>("commitId", options.Environment),
                                        new KeyValuePair<string, object>("namespaces.this_namespace_root", options.NamespaceRoot),
                                        new KeyValuePair<string, object>("telemetry.sdk.language", "CSharp") });

        return builder;

     }

}
