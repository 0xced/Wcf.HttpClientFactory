using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using ServiceReference;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace Wcf.HttpClientFactory.Tests;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by Xunit")]
public class LearnWebservicesFixture(IMessageSink messageSink) : ContainerFixture<LearnWebservicesBuilder, LearnWebservicesContainer>(messageSink)
{
    private bool _useDocker;

    public Uri WebServiceUri => _useDocker ? Container.WebServiceUri : HelloEndpointClient.DefaultUri;

    public bool IsServiceAvailable { get; private set; }

    protected override async ValueTask InitializeAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync($"{HelloEndpointClient.DefaultUri}?wsdl", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            IsServiceAvailable = true;
        }
        catch
        {
            _useDocker = true;
        }

        var useDockerString = Environment.GetEnvironmentVariable("WCF_HTTPCLIENTFACTORY_TESTS_USE_DOCKER");
        if (_useDocker || (bool.TryParse(useDockerString, out _useDocker) && _useDocker))
        {
            await base.InitializeAsync();
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_useDocker)
        {
            await base.DisposeAsyncCore();
        }
    }
}