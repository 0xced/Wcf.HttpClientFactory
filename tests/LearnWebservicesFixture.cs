using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Logging;
using ServiceReference;
using Xunit;
using Xunit.Abstractions;

namespace Wcf.HttpClientFactory.Tests;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by Xunit")]
public class LearnWebservicesFixture(IMessageSink messageSink) : IAsyncLifetime
{
    private LearnWebservicesContainer? _container;

    public Uri WebServiceUri => _container?.WebServiceUri ?? HelloEndpointClient.DefaultUri;

    public bool IsServiceAvailable { get; private set; }

    async Task IAsyncLifetime.InitializeAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            await httpClient.GetAsync(HelloEndpointClient.DefaultUri);
            IsServiceAvailable = true;
        }
        catch (Exception)
        {
            await StartContainerAsync();
            return;
        }

        var useDockerString = Environment.GetEnvironmentVariable("WCF_HTTPCLIENTFACTORY_TESTS_USE_DOCKER");
        if (bool.TryParse(useDockerString, out var useDocker) && useDocker)
        {
            await StartContainerAsync();
        }
    }

    private Task StartContainerAsync()
    {
        var logger = new XUnitLogger(nameof(LearnWebservicesFixture), messageSink, null);
        _container = new LearnWebservicesBuilder(logger).Build();
        return _container.StartAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return _container?.StopAsync() ?? Task.CompletedTask;
    }

    private sealed class LearnWebservicesBuilder : ContainerBuilder<LearnWebservicesBuilder, LearnWebservicesContainer, ContainerConfiguration>
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

    private sealed class LearnWebservicesContainer(IContainerConfiguration configuration) : DockerContainer(configuration)
    {
        public Uri WebServiceUri => new($"http://localhost:{GetMappedPublicPort(8080)}/services/hello");
    }
}
