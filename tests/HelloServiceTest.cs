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

public sealed class HelloServiceTest(LearnWebservicesFixture fixture, ITestOutputHelper outputHelper) : IClassFixture<LearnWebservicesFixture>, IDisposable
{
    private readonly WcfEventListener _eventListener = new(outputHelper);

    public void Dispose() => _eventListener.Dispose();

    [SkippableTheory]
    [CombinatorialData]
    public async Task TestSayHello(ServiceLifetime lifetime, bool registerChannelFactory, bool useDefaultUrl, bool configureMessageHandler)
    {
        Skip.If(useDefaultUrl && !fixture.IsServiceAvailable, "Can't use the default URL when the service is not available");

        var configuration = new Dictionary<string, string?>
        {
            [$"HelloService:{nameof(HelloOptions.Url)}"] = useDefaultUrl ? null : fixture.WebServiceUri.AbsoluteUri,
            [$"HelloService:{nameof(HelloOptions.ConfigureMessageHandler)}"] = configureMessageHandler.ToString(),
        };
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
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

        interceptor.Requests.Should().HaveCount(configureMessageHandler ? 4 : 0);
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "It can't be get-only for configuration binding")]
    private class HelloOptions
    {
        public bool ConfigureMessageHandler { get; init; }
        public Uri? Url { get; init; } = null;
        public string UserName { get; init; } = "AzureDiamond";
        public string Password { get; init; } = "hunter2";
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "It's instantiated through the dependency injection container")]
    private class HelloConfiguration(IOptions<HelloOptions> options) : ContractConfiguration<HelloEndpoint>
    {
        protected override Binding GetBinding()
        {
            var url = options.Value.Url;
            var mode = url?.Scheme == "http" ? BasicHttpSecurityMode.None : BasicHttpSecurityMode.Transport;
            return new BasicHttpBinding { AllowCookies = true, Security = { Mode = mode } };
        }

        protected override EndpointAddress GetEndpointAddress()
        {
            var url = options.Value.Url;
            return url == null ? base.GetEndpointAddress() : new EndpointAddress(url);
        }

        protected override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            _ = endpoint;
            var credential = clientCredentials.UserName;
            credential.UserName = options.Value.UserName;
            credential.Password = options.Value.Password;
        }

        protected override bool ConfigureSocketsHttpHandler(SocketsHttpHandler socketsHttpHandler)
        {
            socketsHttpHandler.PooledConnectionLifetime = TimeSpan.FromHours(2);
            return options.Value.ConfigureMessageHandler;
        }
    }

    private class InterceptingHandler : DelegatingHandler
    {
        private readonly List<HttpRequestMessage> _requests = [];

        public IEnumerable<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return base.SendAsync(request, cancellationToken);
        }
    }
}