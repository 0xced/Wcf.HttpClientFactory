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

namespace System.ServiceModel.HttpClientFactory.Tests;

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

    [SkippableTheory]
    [CombinatorialData]
    public async Task TestSayHello(ServiceLifetime contractLifetime, ServiceLifetime? channelFactoryLifetime)
    {
        Skip.If(contractLifetime == ServiceLifetime.Transient && channelFactoryLifetime == ServiceLifetime.Transient, "Transient contract + channel factory is not supported");

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
        services.AddOptions<HelloOptions>().BindConfiguration("HelloService");
        services.AddContract<HelloEndpoint, HelloConfiguration>(contractLifetime, channelFactoryLifetime);
        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var name in new[] { "Jane", "Steve" })
        {
            var service = serviceProvider.GetRequiredService<HelloEndpoint>();

            var response = await service.SayHelloAsync(new SayHello(new helloRequest { Name = name }));

            response.HelloResponse.Message.Should().Be($"Hello {name}!");
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

        public override EndpointAddress GetEndpointAddress()
        {
            var url = _options.Value.Url;
            return url == null ? base.GetEndpointAddress() : new EndpointAddress(url);
        }

        public override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            var options = _options.Value;
            var credential = clientCredentials.UserName;
            credential.UserName = options.UserName;
            credential.Password = options.Password;
        }
    }

    [SkippableTheory]
    [CombinatorialData]
    public async Task TestCalculator(ServiceLifetime contractLifetime, ServiceLifetime? channelFactoryLifetime)
    {
        Skip.If(contractLifetime == ServiceLifetime.Transient && channelFactoryLifetime == ServiceLifetime.Transient, "Transient contract + channel factory is not supported");

        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddContract<CalculatorSoap>(contractLifetime, channelFactoryLifetime);
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