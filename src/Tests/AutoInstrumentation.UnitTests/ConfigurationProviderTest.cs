using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;

namespace AutoInstrumentation.UnitTests;

[TestClass]
public sealed class ConfigurationProviderTest
{
    [TestMethod]
    public void EmptyConfiguration()
    {
        var config = ConfigurationProvider.GetConfiguration();
        config.Include.Should().BeEmpty();
        config.Exclude.Should().BeEmpty();
        ConfigurationProvider.ToJson(config).Should().NotBeEmpty();
    }
    
    [TestMethod]
    public void Configuration_By_NamespaceListEnvVar()
    {
        using var envVarScope = new DisposableEnvVar("OTEL_DOTNET_AUTO_NAMESPACES", "Digma,System");

        var config = ConfigurationProvider.GetConfiguration();
        config.Include.Should().HaveCount(2);
        config.Include[0].Namespaces = "Digma*";
        config.Include[1].Namespaces = "System*";
        config.Exclude.Should().BeEmpty();
    }
    
    [TestMethod]
    public void Configuration_By_DefaultRuleFileLocation()
    {
        using var file = new DisposableFile("autoinstrumentation.rules.json", 
        @"{
            ""include"":[
                {
                    ""namespaces"": ""Digma.*""
                }
            ] 
        }");

        var config = ConfigurationProvider.GetConfiguration();
        config.Include.Should().HaveCount(1);
        config.Include[0].Namespaces.Should().Be("Digma.*");
        config.Exclude.Should().BeEmpty();
    }
    
    [TestMethod]
    public void Configuration_By_CustomRuleFileLocation()
    {
        using var envVar = new DisposableEnvVar("DIGMA_AUTOINST_RULES_FILE", "myrulefile.json");
        using var file = new DisposableFile("myrulefile.json", 
        @"{
            ""include"":[
                {
                    ""namespaces"": ""Digma.*""
                }
            ] 
        }");

        var config = ConfigurationProvider.GetConfiguration();
        config.Include.Should().HaveCount(1);
        config.Include[0].Namespaces.Should().Be("Digma.*");
        config.Exclude.Should().BeEmpty();
    }

    [TestMethod]
    public void Validate_Fails_OnMissingNamespaces()
    {
        var config = new Configuration
        {
            Include = new[]{new InstrumentationRule()}
        };
        
        var isValid = ConfigurationProvider.Validate(config, out var errors);

        isValid.Should().BeFalse();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("include[0].namespaces: Namespaces matcher must be specified");
    }

    [TestMethod]
    public void Validate_Fails_OnInvalidNamespacesRegex()
    {
        var config = new Configuration
        {
            Include = new[]{new InstrumentationRule{Namespaces = "/[/"}}
        };
        
        var isValid = ConfigurationProvider.Validate(config, out var errors);

        isValid.Should().BeFalse();
        errors.Should().HaveCount(1);
#if NETFRAMEWORK
        errors[0].Should().Be("include[0].namespaces: parsing \"/[/\" - Unterminated [] set.");
#else
        errors[0].Should().Be("include[0].namespaces: Invalid pattern '/[/' at offset 3. Unterminated [] set.");
#endif
    }

    [TestMethod]
    public void Validate_Fails_OnInvalidClassRegex()
    {
        var config = new Configuration
        {
            Include = new[]{new InstrumentationRule{Namespaces = "*", Classes = "/[/"}}
        };
        
        var isValid = ConfigurationProvider.Validate(config, out var errors);

        isValid.Should().BeFalse();
        errors.Should().HaveCount(1);
#if NETFRAMEWORK
        errors[0].Should().Be("include[0].classes: parsing \"/[/\" - Unterminated [] set.");
#else
        errors[0].Should().Be("include[0].classes: Invalid pattern '/[/' at offset 3. Unterminated [] set.");
#endif
    }
    
    [TestMethod]
    public void Validate_Fails_OnInvalidMethodsRegex()
    {
        var config = new Configuration
        {
            Include = new[]{ new InstrumentationRule{Namespaces = "*", Methods = "/[/"}}
        };
        
        var isValid = ConfigurationProvider.Validate(config, out var errors);

        isValid.Should().BeFalse();
        errors.Should().HaveCount(1);
        
#if NETFRAMEWORK
        errors[0].Should().Be("include[0].methods: parsing \"/[/\" - Unterminated [] set.");
#else
        errors[0].Should().Be("include[0].methods: Invalid pattern '/[/' at offset 3. Unterminated [] set.");
#endif
    }
        
    [TestMethod]
    public void Validate_Fails_OnMultipleInvalidRules()
    {
        var config = new Configuration
        {
            Include = new[]{
                new InstrumentationRule{Namespaces = "*", Classes = "/[/"},
                new InstrumentationRule{Namespaces = "*"},
                new InstrumentationRule{Namespaces = "*", Methods = "/[/"}
            }
        };
        
        var isValid = ConfigurationProvider.Validate(config, out var errors);

        isValid.Should().BeFalse();
        errors.Should().HaveCount(2);
#if NETFRAMEWORK
        errors[0].Should().Be("include[0].classes: parsing \"/[/\" - Unterminated [] set.");
        errors[1].Should().Be("include[2].methods: parsing \"/[/\" - Unterminated [] set.");
#else        
        errors[0].Should().Be("include[0].classes: Invalid pattern '/[/' at offset 3. Unterminated [] set.");
        errors[1].Should().Be("include[2].methods: Invalid pattern '/[/' at offset 3. Unterminated [] set.");
#endif
    }
    
    private class DisposableFile : IDisposable
    {
        private readonly string _path;

        public DisposableFile(string path, string content)
        {
            _path = path;
            File.WriteAllText(path, content);
        }
        
        public void Dispose()
        {
            File.Delete(_path);
        }
    }
    
    private class DisposableEnvVar: IDisposable
    {
        private readonly string _name;

        public DisposableEnvVar(string name, string value)
        {
            _name = name;
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, "");
        }
    }
}