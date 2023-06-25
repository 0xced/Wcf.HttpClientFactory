using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceReference;
using Xunit;
using Xunit.Abstractions;

namespace System.ServiceModel.HttpClientFactory.Tests;

public class UnitTest : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly AssertionScope _assertionScope = new();

    static UnitTest()
    {
        // Can only change it once, els e => System.InvalidOperationException: This value cannot be changed after the first ClientBase of type 'ServiceReference.CalculatorSoap' has been created.
        var cacheSettingString = Environment.GetEnvironmentVariable("SYSTEM_SERVICEMODEL_HTTPCLIENTFACTORY_TESTS_CACHESETTING");
        if (Enum.TryParse<CacheSetting>(cacheSettingString, ignoreCase: true, out var cacheSetting))
        {
            HelloEndpointClient.CacheSetting = cacheSetting;
            CalculatorSoapClient.CacheSetting = cacheSetting;
        }
    }

    public UnitTest(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

    public void Dispose() => _assertionScope.Dispose();

    [Theory]
    [CombinatorialData]
    public async Task TestSayHello(ServiceLifetime contractLifetime, ServiceLifetime channelFactoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint>(contractLifetime, channelFactoryLifetime);
        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var name in new[] { "Jane", "Steve" })
        {
            var service = serviceProvider.GetRequiredService<HelloEndpoint>();

            var response = await service.SayHelloAsync(new SayHello(new helloRequest { Name = name }));

            response.HelloResponse.Message.Should().Be($"Hello {name}!");
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task TestCalculator(ServiceLifetime contractLifetime, ServiceLifetime channelFactoryLifetime)
    {
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