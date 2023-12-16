using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
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
using Xunit.Abstractions;

namespace Wcf.HttpClientFactory.Tests;

public class B2BServiceTest
{
    static B2BServiceTest() => B2BServiceClient.CacheSetting = CacheSetting.AlwaysOn;

    private readonly ITestOutputHelper _outputHelper;

    public B2BServiceTest(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

    [SkippableTheory]
    [CombinatorialData]
    public async Task TestB2BServiceSuccess(bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
        services.AddOptions<B2BServiceOptions>().BindConfiguration("B2BService");
        services.AddContract<B2BService, B2BServiceConfiguration>(registerChannelFactory: registerChannelFactory);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var options = serviceProvider.GetRequiredService<IOptions<B2BServiceOptions>>().Value;
        Skip.If(string.IsNullOrEmpty(options.User), $"The B2BService:{nameof(B2BServiceOptions.User)} environment variable must be configured");
        Skip.If(string.IsNullOrEmpty(options.Password), $"The B2BService:{nameof(B2BServiceOptions.Password)} environment variable must be configured");
        Skip.If(string.IsNullOrEmpty(options.BillerId), $"The B2BService:{nameof(B2BServiceOptions.BillerId)} environment variable must be configured");

        for (var i = 1; i <= 2; i++)
        {
            var service = scope.ServiceProvider.GetRequiredService<B2BService>();
            var response = await service.ExecutePingAsync(BillerID: options.BillerId, eBillAccountID: "", ErrorTest: false, ExceptionTest: false);
            response.Should().Be(options.BillerId);
        }
    }

    [SkippableTheory]
    [CombinatorialData]
    public async Task TestB2BServiceError(bool asyncScope, bool registerChannelFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(_outputHelper));
        var configuration = new Dictionary<string, string?>
        {
            [$"B2BService:{nameof(B2BServiceOptions.User)}"] = "wrong-user",
            [$"B2BService:{nameof(B2BServiceOptions.Password)}"] = "wrong-password",
        };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());
        services.AddOptions<B2BServiceOptions>().BindConfiguration("B2BService");
        services.AddContract<B2BService, B2BServiceConfiguration>(registerChannelFactory: registerChannelFactory);

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
            try
            {
                await scope.DisposeAsync();
            }
            catch (CommunicationObjectFaultedException)
            {
                Skip.If(registerChannelFactory, "ServiceChannelProxy should implement IAsyncDisposable but currently doesn't, see https://github.com/dotnet/wcf/issues/5270");
                throw;
            }
        }
        else
        {
            // Dispose throws ¯\_(ツ)_/¯
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
    private class B2BServiceConfiguration : ContractConfiguration<B2BService>
    {
        private readonly IOptions<B2BServiceOptions> _options;

        public B2BServiceConfiguration(IOptions<B2BServiceOptions> options) => _options = options;

        protected override EndpointAddress GetEndpointAddress()
        {
            var url = _options.Value.Url;
            return url == null ? base.GetEndpointAddress() : new EndpointAddress(url);
        }

        protected override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            var options = _options.Value;
            var credentials = clientCredentials.UserName;
            credentials.UserName = options.User;
            credentials.Password = options.Password;
        }
    }
}