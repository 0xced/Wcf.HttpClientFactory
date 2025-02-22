using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Microsoft.Extensions.Logging;

namespace Wcf.HttpClientFactory.Tests;

public sealed class LearnWebservicesBuilder : ContainerBuilder<LearnWebservicesBuilder, LearnWebservicesContainer, ContainerConfiguration>
{
    public LearnWebservicesBuilder(ILogger logger) : this(new ContainerConfiguration())
    {
        DockerResourceConfiguration = Init().WithLogger(logger).DockerResourceConfiguration;
    }

    private LearnWebservicesBuilder(ContainerConfiguration configuration) : base(configuration)
    {
        DockerResourceConfiguration = configuration;
    }

    protected override ContainerConfiguration DockerResourceConfiguration { get; }

    protected override LearnWebservicesBuilder Init()
    {
        return base.Init()
            .WithImage("vicziani/lwsapp")
            .WithPortBinding(8080, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080));
    }

    public override LearnWebservicesContainer Build()
    {
        Validate();
        return new LearnWebservicesContainer(DockerResourceConfiguration);
    }

    protected override LearnWebservicesBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new ContainerConfiguration(resourceConfiguration));
    }

    protected override LearnWebservicesBuilder Clone(IContainerConfiguration resourceConfiguration)
    {
        return Merge(DockerResourceConfiguration, new ContainerConfiguration(resourceConfiguration));
    }

    protected override LearnWebservicesBuilder Merge(ContainerConfiguration oldValue, ContainerConfiguration newValue)
    {
        return new LearnWebservicesBuilder(new ContainerConfiguration(oldValue, newValue));
    }
}