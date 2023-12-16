using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Wcf.HttpClientFactory.Tests;

public class AddContractTest
{
    [Fact]
    public void TestInvalidContractType()
    {
        var services = new ServiceCollection();
        var action = () => services.AddContract<IServiceCollection>();
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("*ServiceContract*");
    }
}