using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceReference;
using Xunit;

namespace Wcf.HttpClientFactory.Tests;

public class B2BServiceTest(ITestOutputHelper outputHelper)
{
    [Theory]
    [CombinatorialData]
    public async Task TestB2BServiceSuccess(ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
        services.AddOptions<B2BServiceOptions>().BindConfiguration("B2BService");
        services.AddContract<B2BService, B2BServiceConfiguration>(factoryLifetime: factoryLifetime);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var options = serviceProvider.GetRequiredService<IOptions<B2BServiceOptions>>().Value;
        Assert.SkipWhen(string.IsNullOrEmpty(options.User), $"The B2BService:{nameof(B2BServiceOptions.User)} environment variable must be configured");
        Assert.SkipWhen(string.IsNullOrEmpty(options.Password), $"The B2BService:{nameof(B2BServiceOptions.Password)} environment variable must be configured");
        Assert.SkipWhen(string.IsNullOrEmpty(options.BillerId), $"The B2BService:{nameof(B2BServiceOptions.BillerId)} environment variable must be configured");

        for (var i = 1; i <= 2; i++)
        {
            var service = scope.ServiceProvider.GetRequiredService<B2BService>();
            var response = await service.ExecutePingAsync(BillerID: options.BillerId, eBillAccountID: "", ErrorTest: false, ExceptionTest: false);
            response.Should().Be(options.BillerId);
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task TestB2BServiceError(bool asyncScope, ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
        var configuration = new Dictionary<string, string?>
        {
            [$"B2BService:{nameof(B2BServiceOptions.User)}"] = "wrong-user",
            [$"B2BService:{nameof(B2BServiceOptions.Password)}"] = "wrong-password",
        };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());
        services.AddOptions<B2BServiceOptions>().BindConfiguration("B2BService");
        services.AddContract<B2BService, B2BServiceConfiguration>(factoryLifetime: factoryLifetime);

        await using var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<B2BService>();

        var executePingAsync = () => service.ExecutePingAsync(BillerID: "", eBillAccountID: "", ErrorTest: false, ExceptionTest: false);
        (await executePingAsync.Should().ThrowExactlyAsync<MessageSecurityException>())
            .WithInnerExceptionExactly<FaultException>()
            .WithMessage("Unknown Username or incorrect Password");

        var state = (service as ICommunicationObject)?.State;
        state.Should().Be(CommunicationState.Faulted);

        if (asyncScope)
        {
            // DisposeAsync works fine, even when in a faulted state thanks to https://github.com/dotnet/wcf/pull/4865
            await scope.DisposeAsync();
        }
        else
        {
            // Dispose throws "by design" ¯\_(ツ)_/¯
            var dispose = () => scope.Dispose();
            dispose.Should().ThrowExactly<CommunicationObjectFaultedException>();
        }
    }

    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "It can't be get-only for configuration binding")]
    private class B2BServiceOptions
    {
        public string? Url { get; init; } = null;
        public string User { get; init; } = "";
        public string Password { get; init; } = "";
        public string BillerId { get; init; } = "";
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "It's instantiated through the dependency injection container")]
    private class B2BServiceConfiguration(IOptions<B2BServiceOptions> options) : ContractConfiguration<B2BService>
    {
        protected override Binding GetBinding() => B2BServiceClient.DefaultBinding;

        protected override EndpointAddress GetEndpointAddress()
        {
            var url = options.Value.Url;
            return url != null ? new EndpointAddress(url) : B2BServiceClient.DefaultEndpointAddress;
        }

        protected override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            var credentials = clientCredentials.UserName;
            credentials.UserName = options.Value.User;
            credentials.Password = options.Value.Password;
        }
    }
}