using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceReference;
using Xunit;

namespace Wcf.HttpClientFactory.Tests;

public class CalculatorServiceTest
{
    [Theory]
    [CombinatorialData]
    public async Task TestCalculatorSuccess(ServiceLifetime contractLifetime, ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddContract<CalculatorSoap, CalculatorConfiguration>("Calculator", contractLifetime, factoryLifetime);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        foreach (var number in new[] { 3, 14, 15 })
        {
            var service = scope.ServiceProvider.GetRequiredService<CalculatorSoap>();
            var response = await service.AddAsync(number, 1);
            response.Should().Be(number + 1);
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task TestCalculatorError(ServiceLifetime contractLifetime, ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddContract<CalculatorSoap, CalculatorConfiguration>("Calculator", contractLifetime, factoryLifetime);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<CalculatorSoap>();

        var action = () => service.DivideAsync(0, 0);
        await action.Should().ThrowExactlyAsync<FaultException>().WithMessage("*overflow*");
    }

    private class CalculatorConfiguration : ContractConfiguration<CalculatorSoap>
    {
        protected override Binding GetBinding() => CalculatorSoapClient.DefaultBinding;

        protected override EndpointAddress GetEndpointAddress() => CalculatorSoapClient.DefaultEndpointAddress;
    }
}