using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using ServiceReference;
using Xunit;

namespace System.ServiceModel.HttpClientFactory.Tests;

public class UnitTest : IDisposable
{
    private readonly AssertionScope _assertionScope;

    public UnitTest()
    {
        _assertionScope = new AssertionScope();
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.SetData("System.ServiceModel.HttpClientFactory.CreateClientBase", null);
        AppDomain.CurrentDomain.SetData("System.ServiceModel.HttpClientFactory.CacheChannelFactory", null);

        _assertionScope.Dispose();
    }

    [Theory]
    [CombinatorialData]
    public async Task TestSayHello(bool createClientBase, bool cacheChannelFactory, ServiceLifetime serviceLifetime)
    {
        AppContext.SetSwitch("System.ServiceModel.HttpClientFactory.CreateClientBase", createClientBase);
        AppContext.SetSwitch("System.ServiceModel.HttpClientFactory.CacheChannelFactory", cacheChannelFactory);

        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint>(serviceLifetime);
        await using var serviceProvider = services.BuildServiceProvider();

        foreach (var name in new[] { "Jane", "Steve" })
        {
            var service = serviceProvider.GetRequiredService<HelloEndpoint>();

            var response = await service.SayHelloAsync(new SayHello(new helloRequest { Name = name }));

            response.HelloResponse.Message.Should().Be($"Hello {name}!");
        }
    }
}