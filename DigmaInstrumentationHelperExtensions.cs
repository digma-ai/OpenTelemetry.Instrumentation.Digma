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

using System.Reflection;

namespace OpenTelemetry.Instrumentation.Digma;

using System.Diagnostics;
using OpenTelemetry.Resources;

public static class DigmaInstrumentationHelperExtensions
{
    private static readonly HashSet<string> IgnoreNamespaces = new() {"Microsoft", "System"};

    public static ResourceBuilder AddDigmaAttributes(this ResourceBuilder builder,
                                                     Action<DigmaConfigurationOptions> configure = null)
    {
        var workingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        DigmaConfigurationOptions options = new DigmaConfigurationOptions();
        if (configure != null)
        {
            configure(options);
        }

        //If namespace not provided try to get it from the calling method
        if (string.IsNullOrEmpty(options.NamespaceRoot))
        {
            StackTrace stackTrace = new StackTrace();
            options.NamespaceRoot = stackTrace?.GetFrame(1)?.GetMethod()?.DeclaringType?.Namespace ?? "";
        }

        if (string.IsNullOrEmpty(options.NamespaceRoot))
        {
            options.NamespaceRoot = Assembly.GetCallingAssembly().GetTypes()
                .Where(x => x.Namespace != null)
                .Select(x => x.Namespace!.Split('.').First())
                .Except(IgnoreNamespaces)
                .Distinct()
                .FirstOrDefault();
        }

        if (options.CommitId == null)
        {
            options.CommitId = Environment.GetEnvironmentVariable(options.CommitIdEnvVariable) ?? "";
        }

        if (options.Environment == null)
        {
            var env = Environment.GetEnvironmentVariable(options.EnvironmentEnvVariable);
            if (env is null)
            {
                options.Environment = Environment.MachineName + "[local]";
            }
            else
            {
                options.Environment = env;
            }
        }

        builder.AddAttributes(new[]
        {
            new KeyValuePair<string, object>("deployment.environment", options.Environment),
            new KeyValuePair<string, object>("paths.working_directory", workingDirectory),
            new KeyValuePair<string, object>("scm.commit.id", options.CommitId),
            new KeyValuePair<string, object>("code.namespace.root", options.NamespaceRoot),
            new KeyValuePair<string, object>("host.name", Environment.MachineName),
            new KeyValuePair<string, object>("digma.span_mapping_pattern", options.SpanMappingPattern),
            new KeyValuePair<string, object>("digma.span_mapping_replacement", options.SpanMappingReplacement),
        });
        return builder;
    }
}
