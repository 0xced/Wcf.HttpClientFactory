using System.ServiceModel;
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
    public async Task TestCalculatorSuccess(ServiceLifetime lifetime, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddContract<CalculatorSoap, ContractConfiguration<CalculatorSoap>>("Calculator", lifetime, registerChannelFactory);
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
    public async Task TestCalculatorError(ServiceLifetime lifetime, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddContract<CalculatorSoap, ContractConfiguration<CalculatorSoap>>("Calculator", lifetime, registerChannelFactory);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<CalculatorSoap>();

        var action = () => service.DivideAsync(0, 0);
        await action.Should().ThrowExactlyAsync<FaultException>().WithMessage("*overflow*");
    }
}