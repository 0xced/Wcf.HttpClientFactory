using System;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceReference;
using Xunit;
using Xunit.Abstractions;

namespace Wcf.HttpClientFactory.Tests;

public sealed class UnitTest : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly AssertionScope _assertionScope = new();
    private readonly WcfEventListener _eventListener;

    static UnitTest()
    {
        // Can only change it once, els e => System.InvalidOperationException: This value cannot be changed after the first ClientBase of type 'ServiceReference.CalculatorSoap' has been created.
        var cacheSettingString = Environment.GetEnvironmentVariable("SYSTEM_SERVICEMODEL_HTTPCLIENTFACTORY_TESTS_CACHESETTING") ?? nameof(CacheSetting.AlwaysOn);
        if (Enum.TryParse<CacheSetting>(cacheSettingString, ignoreCase: true, out var cacheSetting))
        {
            HelloEndpointClient.CacheSetting = cacheSetting;
            CalculatorSoapClient.CacheSetting = cacheSetting;
        }
    }

    public UnitTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _eventListener = new WcfEventListener(outputHelper);
    }

    public void Dispose()
    {
        _eventListener.Dispose();
        _assertionScope.Dispose();
    }

    [Theory]
    [CombinatorialData]
    public async Task TestSayHello(ServiceLifetime lifetime, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
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

    [Theory]
    [CombinatorialData]
    public async Task TestCalculator(ServiceLifetime lifetime, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddContract<CalculatorSoap>("Calculator", lifetime, registerChannelFactory);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        foreach (var number in new[] { 3, 14, 15 })
        {
            var service = scope.ServiceProvider.GetRequiredService<CalculatorSoap>();

            var response = await service.AddAsync(number, 1);

            response.Should().Be(number + 1);
        }
    }
}