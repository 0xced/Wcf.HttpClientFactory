using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
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
    public void AddContract_ConcreteContractType_Throws()
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<HelloEndpointClient, HelloClientConfiguration>();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("TContract")
            .WithMessage("The contract type (HelloEndpointClient) must be an interface type. (Parameter 'TContract')");
    }

    [Fact]
    public void AddContract_CallTwice_Throws()
    {
        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint, HelloConfiguration>();
        var action = () => services.AddContract<HelloEndpoint, HelloConfiguration>();
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("TContract")
            .WithMessage("The AddContract<HelloEndpoint, HelloConfiguration>() method must be called only once and it was already called (with a transient lifetime). (Parameter 'TContract')");
    }

    [Fact]
    public void AddContract_InvalidContractConfiguration_Throws()
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<CalculatorSoap, ContractConfiguration<CalculatorSoap>>();

        action.Should().Throw<ArgumentException>()
            .WithParameterName("TConfiguration")
            .WithMessage("The configuration class (ContractConfiguration<CalculatorSoap>) is abstract, it must be subclassed. (Parameter 'TConfiguration')");
    }
}

public class HelloConfiguration : ContractConfiguration<HelloEndpoint>
{
    protected override Binding GetBinding() => throw new NotSupportedException("Should not be called");
    protected override EndpointAddress GetEndpointAddress() => throw new NotSupportedException("Should not be called");
}

public class HelloClientConfiguration : ContractConfiguration<HelloEndpointClient>
{
    protected override Binding GetBinding() => throw new NotSupportedException("Should not be called");
    protected override EndpointAddress GetEndpointAddress() => throw new NotSupportedException("Should not be called");
}
