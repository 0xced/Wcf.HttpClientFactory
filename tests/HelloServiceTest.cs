using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
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

public sealed class HelloServiceTest : IDisposable
{
    static HelloServiceTest()
    {
        HelloEndpointClient.CacheSetting = CacheSetting.AlwaysOn;
    }

    private readonly ITestOutputHelper _outputHelper;
    private readonly WcfEventListener _eventListener;

    public HelloServiceTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _eventListener = new WcfEventListener(outputHelper);
    }

    public void Dispose()
    {
        _eventListener.Dispose();
    }

    [Theory]
    [CombinatorialData]
    public async Task TestSayHello(ServiceLifetime lifetime, bool registerChannelFactory,
        [CombinatorialValues("https://apps.learnwebservices.com/services/hello", null)] string? url)
    {
        var configuration = new Dictionary<string, string?> { [$"HelloService:{nameof(HelloOptions.Url)}"] = url };
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());
        services.AddOptions<HelloOptions>().BindConfiguration("HelloService");
        services.AddContract<HelloEndpoint, HelloConfiguration>("Hello", lifetime, registerChannelFactory);
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
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "It can't be get-only for configuration binding")]
    private class HelloOptions
    {
        public string? Url { get; init; } = null;
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
            return new BasicHttpBinding { AllowCookies = true, Security = { Mode = BasicHttpSecurityMode.Transport } };
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
}