using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceReference;
using Xunit;

namespace Wcf.HttpClientFactory.Tests;

public class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddContract_InvalidContractType_Throws()
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<IServiceCollection, ContractConfiguration<IServiceCollection>>();
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("TContract")
            .WithMessage("Attempted to get contract type for IServiceCollection, but that type is not a ServiceContract, nor does it inherit a ServiceContract. (Parameter 'TContract')");
    }

    [Fact]

    public void AddContract_CallTwice_Throws()
    {
        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint, ContractConfiguration<HelloEndpoint>>();
        var action = () => services.AddContract<HelloEndpoint, ContractConfiguration<HelloEndpoint>>();
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("TContract")
            .WithMessage("The AddContract<HelloEndpoint, ContractConfiguration<HelloEndpoint>>() method must be called only once and it was already called (with a Transient lifetime) (Parameter 'TContract')");
    }

    [Fact]
    public void AddContract_ContractTypeImplementation_Throws()
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<CalculatorSoapClient, ContractConfiguration<CalculatorSoapClient>>(registerChannelFactory: false);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("TContract")
            .WithMessage("No ClientBase<ServiceReference.CalculatorSoapClient> were found in the CalculatorService assembly, try with AddContract<CalculatorSoap, ContractConfiguration<CalculatorSoap>>() instead (Parameter 'TContract')");
    }

    [Fact]
    public void AddContract_ContractTypeImplementationWithInheritedConfiguration_Throws()
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<CalculatorSoapClient, CalculatorConfiguration>(registerChannelFactory: false);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("TContract")
            .WithMessage("No ClientBase<ServiceReference.CalculatorSoapClient> were found in the CalculatorService assembly, try with AddContract<CalculatorSoap, CalculatorConfiguration>() instead and make CalculatorConfiguration inherit from ContractConfiguration<CalculatorSoap> (Parameter 'TContract')");
    }
}

internal class CalculatorConfiguration : ContractConfiguration<CalculatorSoapClient>;
