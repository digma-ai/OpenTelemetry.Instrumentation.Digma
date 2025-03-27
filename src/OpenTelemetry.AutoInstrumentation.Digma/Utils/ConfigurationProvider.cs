using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public static class ConfigurationProvider
{
    private const string RuleFileDefaultName = "autoinstrumentation.rules.json";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public static Configuration GetConfiguration()
    {
        var filePath = TryGetRulesFilePath();

        if (filePath != null && TryReadConfigurationFromFile(filePath, out var configuration))
        {
            return configuration;
        }

        if (TryBuildConfigurationFromNamespacesList(out configuration))
        {
            return configuration;
        }

        Logger.LogInfo("Using an empty configuration");
        return new Configuration();
    }

    private static bool TryBuildConfigurationFromNamespacesList(out Configuration configuration)
    {
        var rules = EnvVars.OTEL_DOTNET_AUTO_NAMESPACES?
            .Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new InstrumentationRule {Namespaces = x + "*"})
            .ToArray() ?? Array.Empty<InstrumentationRule>();
        if (!rules.Any())
        {
            Logger.LogInfo($"No namespaces were specified in {nameof(EnvVars.OTEL_DOTNET_AUTO_NAMESPACES)}");
            configuration = null;
            return false;
        }

        configuration = new Configuration {Include = rules};
        return true;
    }
    
    private static bool TryReadConfigurationFromFile(string filePath, out Configuration configuration)
    {
        configuration = ReadFromFile(filePath, out var errors);
        if (configuration == null)
        {
            Logger.LogError("Failed to read the rules file:\n" + string.Join("\n", errors));
            return false;
        }

        configuration.Include ??= Array.Empty<InstrumentationRule>();
        configuration.Exclude ??= Array.Empty<InstrumentationRule>();
        
        var isValid = Validate(configuration, out errors);
        if (!isValid)
        {
            Logger.LogError("Invalid rules file:\n" + string.Join("\n", errors));
            configuration = null;
            return false;
        }
        
        return true;
    } 
    
    private static string? TryGetRulesFilePath()
    {
        var filePath = EnvVars.DIGMA_AUTOINST_RULES_FILE;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            if (File.Exists(filePath))
            {
                Logger.LogInfo($"Rules file found at '{filePath}'");
                return filePath;
            }

            Logger.LogInfo($"Rules file was not found at '{filePath}'");
        }
        
        filePath = Path.Combine(Directory.GetCurrentDirectory(), RuleFileDefaultName);
        if (File.Exists(filePath))
        {
            Logger.LogInfo($"Rules file found at '{filePath}'");
            return filePath;
        }

        Logger.LogInfo($"Rules file was not found at its default location '{filePath}'");
        
        return null;
    }
    
    private static Configuration ReadFromFile(string path, out string[] errors)
    {
        errors = Array.Empty<string>();
        try
        {
            var text = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Configuration>(text, JsonSerializerOptions);
        }
        catch (Exception e)
        {
            errors = new[] {e.Message};
            return null;
        }
    }
    
    public static bool Validate(Configuration conf, out string[] errors)
    {
        var errorsList = new List<string>();

        var rules = Enumerable.Empty<(string Location, InstrumentationRule)>()
            .Concat(conf.Include.Select((r, i) => ($"include[{i}]", r)))
            .Concat(conf.Exclude.Select((r, i) => ($"exclude[{i}]", r)));
        
        foreach (var (location, rule) in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Namespaces))
            {
                errorsList.Add($"{location}.namespaces: Namespaces matcher must be specified");
            }
            else if (InstrumentationRule.IsRegex(rule.Namespaces) &&
                !ValidateRegex(rule.Namespaces, out var nsError))
            {
                errorsList.Add($"{location}.namespaces: {nsError}");
            }
            
            if (!string.IsNullOrWhiteSpace(rule.Classes) &&
                InstrumentationRule.IsRegex(rule.Classes) &&
                !ValidateRegex(rule.Classes, out var clError))
            {
                errorsList.Add($"{location}.classes: {clError}");
            }
            
            if (!string.IsNullOrWhiteSpace(rule.Methods) &&
                InstrumentationRule.IsRegex(rule.Methods) &&
                !ValidateRegex(rule.Methods, out var mdError))
            {
                errorsList.Add($"{location}.methods: {mdError}");
            }
        }

        errors = errorsList.ToArray();
        return !errors.Any();
    }

    private static bool ValidateRegex(string pattern, out string error)
    {
        try
        {
            new Regex(pattern);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }
}