using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using MartinCostello.Logging.XUnit;
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
}