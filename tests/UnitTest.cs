using System.ServiceModel.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceReference;
using Xunit;

namespace System.ServiceModel.HttpClientFactory.Tests;

public class ClientConfigurationProvider : IClientConfigurationProvider
{
    public Binding GetBinding(string configurationName)
    {
        return new BasicHttpBinding
        {
            MaxBufferSize = int.MaxValue,
            ReaderQuotas = Xml.XmlDictionaryReaderQuotas.Max,
            MaxReceivedMessageSize = int.MaxValue,
            AllowCookies = true
        };
    }

    public EndpointAddress GetEndpointAddress(string configurationName)
    {
        return new EndpointAddress("http://apps.learnwebservices.com/services/hello");
    }
}

public class UnitTest
{
    private static readonly IServiceProvider ServiceProvider;

    static UnitTest()
    {
        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint>();
        ServiceProvider = services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("Steve", "Hello Steve!")]
    [InlineData("Jane", "Hello Jane!")]
    [InlineData("<>", "Hello <>!")]
    public async Task TestSayHello(string name, string greeting)
    {
        var service = ServiceProvider.GetRequiredService<HelloEndpoint>();

        var response = await service.SayHelloAsync(new SayHello(new helloRequest { Name = name }));

        response.HelloResponse.Message.Should().Be(greeting);
    }
}