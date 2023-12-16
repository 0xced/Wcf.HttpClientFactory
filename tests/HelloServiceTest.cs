using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceReference;
using Xunit;
using Xunit.Abstractions;

namespace Wcf.HttpClientFactory.Tests;

public sealed class HelloServiceTest : IClassFixture<LearnWebservicesFixture>, IDisposable
{
    static HelloServiceTest()
    {
        HelloEndpointClient.CacheSetting = CacheSetting.AlwaysOn;
    }

    private readonly LearnWebservicesFixture _fixture;
    private readonly ITestOutputHelper _outputHelper;
    private readonly WcfEventListener _eventListener;

    public HelloServiceTest(LearnWebservicesFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
        _eventListener = new WcfEventListener(outputHelper);
    }

    public void Dispose()
    {
        _eventListener.Dispose();
    }

    [SkippableTheory]
    [CombinatorialData]
    public async Task TestSayHello(ServiceLifetime lifetime, bool registerChannelFactory, bool useDefaultUrl)
    {
        Skip.If(useDefaultUrl && !_fixture.IsServiceAvailable, "Can't use the default URL when the service is not available");

        var configuration = new Dictionary<string, string?> { [$"HelloService:{nameof(HelloOptions.Url)}"] = useDefaultUrl ? null : _fixture.WebServiceUri.AbsoluteUri };
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());
        services.AddOptions<HelloOptions>().BindConfiguration("HelloService");
        var interceptor = new InterceptingHandler();
        services.AddContract<HelloEndpoint, HelloConfiguration>("Hello", lifetime, registerChannelFactory).AddHttpMessageHandler(_ => interceptor);
        await using var serviceProvider = services.BuildServiceProvider();

        for (var i = 1; i <= 2; i++)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            foreach (var name in new[] { $"Jane {i}", $"Steve {i}" })
            {
                var service = scope.ServiceProvider.GetRequiredService<HelloEndpoint>();

                var response = await service.SayHelloAsync(new SayHello(new helloRequest { Name = name }));

                response.HelloResponse.Message.Should().Be($"Hello {name}!");
            }
        }

        interceptor.Requests.Should().HaveCount(4);
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "It can't be get-only for configuration binding")]
    private class HelloOptions
    {
        public Uri? Url { get; init; } = null;
        public string UserName { get; init; } = "AzureDiamond";
        public string Password { get; init; } = "hunter2";
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "It's instantiated through the dependency injection container")]
    private class HelloConfiguration : ContractConfiguration<HelloEndpoint>
    {
        private readonly IOptions<HelloOptions> _options;

        public HelloConfiguration(IOptions<HelloOptions> options) => _options = options;

        protected override Binding GetBinding()
        {
            var url = _options.Value.Url;
            var mode = url?.Scheme == "http" ? BasicHttpSecurityMode.None : BasicHttpSecurityMode.Transport;
            return new BasicHttpBinding { AllowCookies = true, Security = { Mode = mode } };
        }

        protected override EndpointAddress GetEndpointAddress()
        {
            var url = _options.Value.Url;
            return url == null ? base.GetEndpointAddress() : new EndpointAddress(url);
        }

        protected override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            _ = endpoint;
            var options = _options.Value;
            var credential = clientCredentials.UserName;
            credential.UserName = options.UserName;
            credential.Password = options.Password;
        }
    }

    private class InterceptingHandler : DelegatingHandler
    {
        private readonly List<HttpRequestMessage> _requests = new();

        public IEnumerable<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return base.SendAsync(request, cancellationToken);
        }
    }
}