using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace AutoInstrumentation.IntegrationTests.Utils;


public sealed class VerticaConfiguration : ContainerConfiguration
{
    public VerticaConfiguration(
        string database = null,
        string username = null,
        string password = null)
    {
        Database = database;
        Username = username;
        Password = password;
    }

    public VerticaConfiguration(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }

    public VerticaConfiguration(IContainerConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }
    
    public VerticaConfiguration(VerticaConfiguration resourceConfiguration)
        : this(new VerticaConfiguration(), resourceConfiguration)
    {
        // Passes the configuration upwards to the base implementations to create an updated immutable copy.
    }
    
    public VerticaConfiguration(VerticaConfiguration oldValue, VerticaConfiguration newValue)
        : base(oldValue, newValue)
    {
        Database = BuildConfiguration.Combine(oldValue.Database, newValue.Database);
        Username = BuildConfiguration.Combine(oldValue.Username, newValue.Username);
        Password = BuildConfiguration.Combine(oldValue.Password, newValue.Password);
    }

    public string Database { get; }

    public string Username { get; }

    public string Password { get; }
}


public class VerticaBuilder : ContainerBuilder<VerticaBuilder, VerticaContainer, VerticaConfiguration>
{
    public const string DefaultImage = "vertica/vertica-ce";
    public const int DefaultPort = 5433;
    public const string DefaultDatabase = "master";
    public const string DefaultUsername = "dbadmin";
    public const string DefaultPassword = "yourStrong(!)Password";


    public VerticaBuilder()
        : this(new VerticaConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }
    
    private VerticaBuilder(VerticaConfiguration resourceConfiguration)
        : base(resourceConfiguration)
    {
        DockerResourceConfiguration = resourceConfiguration;
    }
    
    public override VerticaContainer Build()
    {
        return new VerticaContainer(DockerResourceConfiguration);
    }

    protected override VerticaBuilder Init()
    {
        return base.Init()
            .WithImage(DefaultImage)
            .WithPortBinding(DefaultPort, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithDatabase(DefaultDatabase)
            .WithUsername(DefaultUsername)
            .WithPassword(DefaultPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()));
    }
    
    private VerticaBuilder WithDatabase(string database)
    {
        return Merge(DockerResourceConfiguration, new VerticaConfiguration(database: database))
            .WithEnvironment("VERTICA_DB_NAME", database);
    }

    private VerticaBuilder WithUsername(string username)
    {
        return Merge(DockerResourceConfiguration, new VerticaConfiguration(username: username))
            .WithEnvironment("VERTICA_DB_USER", username);
    }
    private VerticaBuilder WithPassword(string password)
    {
        return Merge(DockerResourceConfiguration, new VerticaConfiguration(password: password))
            .WithEnvironment("VERTICA_DB_PASSWORD", password);
    }
    
    protected override VerticaBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new VerticaConfiguration(resourceConfiguration));
    }
    
    protected override VerticaBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new VerticaConfiguration(resourceConfiguration));
    }
    
    protected override VerticaBuilder Merge(VerticaConfiguration oldValue, VerticaConfiguration newValue)
    {
        return new VerticaBuilder(new VerticaConfiguration(oldValue, newValue));
    }

    protected override VerticaConfiguration DockerResourceConfiguration { get; }

    private sealed class WaitUntil : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            var execResult = await ((VerticaContainer)container).ExecHealthCheck();
            return execResult.ExitCode == 0;
        }
    }
}

public class VerticaContainer : DockerContainer, IDatabaseContainer
{
    private readonly VerticaConfiguration _configuration;

    public VerticaContainer(VerticaConfiguration configuration) : base(configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExecResult> ExecHealthCheck()
    {
        var cmd = new[]
        {
            "/opt/vertica/bin/vsql",
            "-U", _configuration.Username,
            "-w", _configuration.Password,
            "-d", _configuration.Database,
            "-c", "SELECT 1;"
        };

        return await ExecAsync(cmd);
    }
    
    public string GetConnectionString()
    {
        return $"Host=localhost;Port={GetMappedPublicPort(VerticaBuilder.DefaultPort)};Database={_configuration.Database};User={_configuration.Username};Password={_configuration.Password};";
    }
}