using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;

namespace AutoInstrumentation.IntegrationTests.Utils;


public class VerticaBuilder : ContainerBuilder<VerticaBuilder, VerticaContainer, ContainerConfiguration>
{
    private const string DefaultImage = "vertica/vertica-ce";
    private const int DefaultPort = 5433;
    public const string DefaultDatabase = "master";
    public const string DefaultUsername = "sa";
    public const string DefaultPassword = "yourStrong(!)Password";

    public VerticaBuilder()
        : this(new ContainerConfiguration())
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
    }
    
    private VerticaBuilder(ContainerConfiguration resourceConfiguration)
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
            .WithPortBinding(DefaultPort, DefaultPort)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("VERTICA_DB_NAME", DefaultDatabase)
            .WithEnvironment("VERTICA_DB_PASSWORD ", DefaultPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitUntil()));
    }
    
    protected override VerticaBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new ContainerConfiguration(resourceConfiguration));
    }
    
    protected override VerticaBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new ContainerConfiguration(resourceConfiguration));
    }
    
    protected override VerticaBuilder Merge(ContainerConfiguration oldValue, ContainerConfiguration newValue)
    {
        return new VerticaBuilder(new ContainerConfiguration(oldValue, newValue));
    }

    protected override ContainerConfiguration DockerResourceConfiguration { get; }

    private sealed class WaitUntil : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            var execResult = await container.ExecAsync(new[]
                {
                    "/opt/vertica/bin/vsql", "-U", "dbadmin", "-w", DefaultPassword, "-d", DefaultDatabase, "-c", "SELECT 1;"
                })
                .ConfigureAwait(false);

            return execResult.ExitCode == 0;
        }
    }
}

public class VerticaContainer : DockerContainer, IDatabaseContainer
{
    private readonly ContainerConfiguration _configuration;

    public VerticaContainer(ContainerConfiguration configuration) : base(configuration)
    {
        _configuration = configuration;
    }
    
    public string GetConnectionString()
    {
        return "Host=localhost;Port=5433;Database=VMart;User=dbadmin;Password=;";
    }
}