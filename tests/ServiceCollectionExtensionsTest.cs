using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ServiceReference;
using Xunit;

namespace Wcf.HttpClientFactory.Tests;

public class ServiceCollectionExtensionsTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Name")]
    public void AddContract_InvalidContractType_Throws(string? httpClientName)
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<IServiceCollection>(httpClientName);
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("*ServiceContract*");
    }

    [Fact]
    public void AddContract_CallTwice_Throws()
    {
        var services = new ServiceCollection();
        services.AddContract<HelloEndpoint>();
        var action = () => services.AddContract<HelloEndpoint>();
        action.Should().ThrowExactly<ArgumentException>().WithMessage("*already called*").WithParameterName("TContract");
    }
}