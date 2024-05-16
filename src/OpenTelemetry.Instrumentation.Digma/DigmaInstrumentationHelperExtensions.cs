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

using System.Diagnostics;
using System.Net;
using System.Reflection;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Instrumentation.Digma;

public static class DigmaInstrumentationHelperExtensions
{
    private static readonly HashSet<string> IgnoreNamespaces = new HashSet<string>() {"Microsoft", "System"};

    public static ResourceBuilder AddDigmaAttributes(this ResourceBuilder builder,
        Action<DigmaConfigurationOptions>? configure = null)
    {
        var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var hostName = Dns.GetHostName();

        DigmaConfigurationOptions options = new DigmaConfigurationOptions();
        if (configure != null)
        {
            configure(options);
        }

        //If namespace not provided try to get it from the calling method
        if (string.IsNullOrWhiteSpace(options.NamespaceRoot))
        {
            StackTrace stackTrace = new StackTrace();
            options.NamespaceRoot = stackTrace?.GetFrame(1)?.GetMethod()?.DeclaringType?.Namespace ?? "";
        }

        if (string.IsNullOrWhiteSpace(options.NamespaceRoot))
        {
            options.NamespaceRoot = Assembly.GetCallingAssembly().GetTypes()
                .Where(x => x.Namespace != null)
                .Select(x => x.Namespace!.Split('.').First())
                .Except(IgnoreNamespaces)
                .Distinct()
                .FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(options.CommitId))
        {
            options.CommitId = Environment.GetEnvironmentVariable(options.CommitIdEnvVariable) ?? "";
        }

        SetEnvironment(options, builder);
        SetEnvironmentType(options, builder);
        SetUserId(builder, options);

        builder.AddAttributes(new[]
        {
            new KeyValuePair<string, object>("paths.working_directory", workingDirectory),
            new KeyValuePair<string, object>("scm.commit.id", options.CommitId),
            new KeyValuePair<string, object>("code.namespace.root", options.NamespaceRoot),
            new KeyValuePair<string, object>("host.name", hostName),
            new KeyValuePair<string, object>("digma.span_mapping_pattern", options.SpanMappingPattern),
            new KeyValuePair<string, object>("digma.span_mapping_replacement", options.SpanMappingReplacement),
        });
        return builder;
    }

    private static void SetUserId(ResourceBuilder builder, DigmaConfigurationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UserId))
        {
            options.UserId = Environment.GetEnvironmentVariable(options.DigmaUserIdVariable) ?? null;
            if (!string.IsNullOrWhiteSpace(options.UserId))
                builder.AddAttributes(new[] {new KeyValuePair<string, object>("digma.user.id", options.UserId)});
        }
    }

    private static void SetEnvironmentType(DigmaConfigurationOptions options, ResourceBuilder builder)
    {
        if (options.EnvironmentType == null)
        {
            string ?environmentTypeStr = Environment.GetEnvironmentVariable(options.DigmaEnvironmentTypeVariable);
            if (!string.IsNullOrWhiteSpace(environmentTypeStr))
            {
                options.EnvironmentType =  Enum.Parse<EnvironmentType>(environmentTypeStr, true);
            }
        }
        if (options.EnvironmentType != null)
        {
            builder.AddAttributes(new[] {new KeyValuePair<string, object>("digma.environment.type", options.EnvironmentType.ToString())});
        }
    }
    
    private static void SetEnvironment(DigmaConfigurationOptions options, ResourceBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(options.EnvironmentId))
        {
            options.EnvironmentId = Environment.GetEnvironmentVariable(options.DigmaEnvironmentIdVariable) ?? null;
        }
        if (!string.IsNullOrWhiteSpace(options.EnvironmentId))
        {
            builder.AddAttributes(new[]
            {
                new KeyValuePair<string, object>("digma.environment.id", options.EnvironmentId),
            });
            return;
        }
        
        
        if (string.IsNullOrWhiteSpace(options.Environment))
        {
            options.Environment = Environment.GetEnvironmentVariable(options.DigmaEnvironmentEnvVariable) ?? null;
            if (string.IsNullOrWhiteSpace(options.Environment)) 
                options.Environment = Environment.GetEnvironmentVariable(options.EnvironmentEnvVariable) ?? null;
        }
        
        if (!string.IsNullOrWhiteSpace(options.Environment))
        {
            builder.AddAttributes(new[] {new KeyValuePair<string, object>("digma.environment", options.Environment)});
        }
    }
}
