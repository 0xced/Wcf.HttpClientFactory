using System;
using System.Linq;
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

public class UnitTest : IDisposable
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
    public async Task TestSayHello(ServiceLifetime contractLifetime, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
        services.AddOptions<HelloOptions>().BindConfiguration("HelloService");
        services.AddContract<HelloEndpoint, HelloConfiguration>(contractLifetime, registerChannelFactory);
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

    private class HelloOptions
    {
        public string? Url { get; init; }
        public string UserName { get; init; } = "AzureDiamond";
        public string Password { get; init; } = "hunter2";
    }

    [HttpClient("Hello")]
    private class HelloConfiguration : ContractConfiguration<HelloEndpoint>
    {
        private readonly IOptions<HelloOptions> _options;

        public HelloConfiguration(IOptions<HelloOptions> options) => _options = options;

        protected override EndpointAddress GetEndpointAddress()
        {
            var url = _options.Value.Url;
            return url == null ? base.GetEndpointAddress() : new EndpointAddress(url);
        }

        protected override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            var options = _options.Value;
            var credential = clientCredentials.UserName;
            credential.UserName = options.UserName;
            credential.Password = options.Password;
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task TestDisposeFaulted(bool asyncScope)
    {
        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint, HelloFaultingConfiguration>();
        await using var serviceProvider = services.BuildServiceProvider();
        var scope = asyncScope ? serviceProvider.CreateAsyncScope() : serviceProvider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<HelloEndpoint>();

        try
        {
            await service.SayHelloAsync(new SayHello(new helloRequest()));
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("CustomBinding"))
        {
        }
        finally
        {
            var state = (service as ICommunicationObject)?.State;
            state.Should().Be(CommunicationState.Faulted);

            if (asyncScope)
            {
                // DisposeAsync works fine, even when in a faulted state thanks to https://github.com/dotnet/wcf/pull/4865
                await ((IAsyncDisposable)scope).DisposeAsync();
            }
            else
            {
                // Dispose throws ¯\_(ツ)_/¯
                var dispose = () => scope.Dispose();
                dispose.Should().ThrowExactly<CommunicationObjectFaultedException>();
            }
        }
    }

    private class HelloFaultingConfiguration : ContractConfiguration<HelloEndpoint>
    {
        protected override Binding GetBinding() => new CustomBinding(base.GetBinding().CreateBindingElements().Append(new ReliableSessionBindingElement()));
    }

    [Theory]
    [CombinatorialData]
    public async Task TestCalculator(ServiceLifetime contractLifetime, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddContract<CalculatorSoap>(contractLifetime, registerChannelFactory);
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