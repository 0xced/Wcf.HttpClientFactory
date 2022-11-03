using System.Collections.Generic;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceReference;
using Xunit;

namespace System.ServiceModel.HttpClientFactory.Tests;

public class UnitTest
{
    private static readonly HelloFactory HelloFactory;

    static UnitTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HelloFactory>();
        services.AddContractHttpClientFactory<HelloEndpoint>();
        HelloFactory = services.BuildServiceProvider().GetRequiredService<HelloFactory>();
    }

    [Theory]
    [InlineData("Steve", "Hello Steve!")]
    [InlineData("Jane", "Hello Jane!")]
    public async Task TestSayHello(string name, string greeting)
    {
        var client = HelloFactory.CreateHelloEndpoint();
        var response = await client.SayHelloAsync(new SayHello(new helloRequest { Name = name }));

        response.HelloResponse.Message.Should().Be(greeting);
    }
}

public class HelloFactory
{
    private readonly IEnumerable<IEndpointBehavior> _endpointBehaviors;

    public HelloFactory(IEnumerable<IEndpointBehavior> endpointBehaviors)
    {
        _endpointBehaviors = endpointBehaviors ?? throw new ArgumentNullException(nameof(endpointBehaviors));
    }

    public HelloEndpoint CreateHelloEndpoint()
    {
        var helloEndpoint = new HelloEndpointClient();
        foreach (var endpointBehavior in _endpointBehaviors)
        {
            helloEndpoint.Endpoint.EndpointBehaviors.Add(endpointBehavior);
        }
        return helloEndpoint;
    }
}