using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
    public async Task B2BService_SyncOnlyConfiguration_Success(ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build());
        services.AddOptions<B2BServiceAuthenticationOptions>().BindConfiguration("B2BService");
        services.AddContract<B2BService, B2BServiceSyncOnlyConfiguration>(factoryLifetime: factoryLifetime);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var options = serviceProvider.GetRequiredService<IOptions<B2BServiceAuthenticationOptions>>().Value;
        Assert.SkipWhen(string.IsNullOrEmpty(options.User), $"The B2BService:{nameof(options.User)} environment variable must be configured");
        Assert.SkipWhen(string.IsNullOrEmpty(options.Password), $"The B2BService:{nameof(options.Password)} environment variable must be configured");
        Assert.SkipWhen(string.IsNullOrEmpty(options.BillerId), $"The B2BService:{nameof(options.BillerId)} environment variable must be configured");

        for (var i = 1; i <= 2; i++)
        {
            var service = scope.ServiceProvider.GetRequiredService<B2BService>();
            var response = await service.ExecutePingAsync(BillerID: options.BillerId, eBillAccountID: "", ErrorTest: false, ExceptionTest: false);
            response.Should().Be(options.BillerId);
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task B2BService_AsyncOnlyConfiguration_Success([CombinatorialValues(ServiceLifetime.Singleton, ServiceLifetime.Scoped)] ServiceLifetime factoryLifetime)
    {
        var keyVaultName = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_NAME");
        Assert.SkipWhen(string.IsNullOrEmpty(keyVaultName), "The AZURE_KEY_VAULT_NAME environment variable must be configured");

        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
        services.AddSingleton(new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net"), new DefaultAzureCredential()));
        services.AddContract<B2BService, B2BServiceAsyncOnlyConfiguration>(factoryLifetime: factoryLifetime);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var channelFactory = scope.ServiceProvider.GetRequiredService<ChannelFactory<B2BService>>();
        await Task.Factory.FromAsync(channelFactory.BeginOpen, channelFactory.EndOpen, null);

        var options = serviceProvider.GetRequiredService<IOptions<B2BServiceOptions>>().Value;

        for (var i = 1; i <= 2; i++)
        {
            var service = scope.ServiceProvider.GetRequiredService<B2BService>();
            var response = await service.ExecutePingAsync(BillerID: options.BillerId, eBillAccountID: "", ErrorTest: false, ExceptionTest: false);
            response.Should().Be(options.BillerId);
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task B2BService_AsyncOnlyConfiguration_OpenAsyncError([CombinatorialValues(ServiceLifetime.Singleton, ServiceLifetime.Scoped)] ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SecretClient(new Uri("about:blank"), new DefaultAzureCredential()));
        services.AddContract<B2BService, B2BServiceAsyncOnlyConfiguration>(factoryLifetime: factoryLifetime);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        // ReSharper disable once AccessToDisposedClosure
        var action = () => _ = scope.ServiceProvider.GetRequiredService<B2BService>();

        const string expectedMessage = "The ChannelFactory<B2BService> should be opened asynchronously prior to instantiating B2BService.";
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage(expectedMessage);
    }

    [Fact]
    public async Task B2BService_AsyncOnlyConfiguration_ImplementConfigureEndpointError()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SecretClient(new Uri("about:blank"), new DefaultAzureCredential()));
        services.AddContract<B2BService, B2BServiceAsyncOnlyConfiguration>(factoryLifetime: ServiceLifetime.Transient);
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        // ReSharper disable once AccessToDisposedClosure
        var action = () => _ = scope.ServiceProvider.GetRequiredService<B2BService>();

        const string expectedMessage = "Please override the ConfigureEndpoint method in B2BServiceTest.B2BServiceAsyncOnlyConfiguration. " +
                                       "Alternatively, the ChannelFactory<B2BService> can be registered as singleton or scoped and be opened asynchronously prior to instantiating B2BService.";
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage(expectedMessage);
    }

    [Theory]
    [CombinatorialData]
    public async Task B2BService_WrongCredentials_Error(bool asyncScope, ServiceLifetime factoryLifetime)
    {
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
        var configuration = new Dictionary<string, string?>
        {
            [$"B2BService:{nameof(B2BServiceAuthenticationOptions.User)}"] = "wrong-user",
            [$"B2BService:{nameof(B2BServiceAuthenticationOptions.Password)}"] = "wrong-password",
        };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());
        services.AddOptions<B2BServiceAuthenticationOptions>().BindConfiguration("B2BService");
        services.AddContract<B2BService, B2BServiceSyncOnlyConfiguration>(factoryLifetime: factoryLifetime);

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
        public string BillerId { get; set; } = "";
    }

    private class B2BServiceAuthenticationOptions : B2BServiceOptions
    {
        public string User { get; init; } = "";
        public string Password { get; init; } = "";
    }

    private abstract class B2BServiceConfiguration(IOptions<B2BServiceOptions> options) : ContractConfiguration<B2BService>
    {
        protected override Binding GetBinding() => B2BServiceClient.DefaultBinding;

        protected override EndpointAddress GetEndpointAddress()
        {
            var url = options.Value.Url;
            return url != null ? new EndpointAddress(url) : B2BServiceClient.DefaultEndpointAddress;
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "It's instantiated through the dependency injection container")]
    private class B2BServiceSyncOnlyConfiguration(IOptions<B2BServiceAuthenticationOptions> options) : B2BServiceConfiguration(options)
    {
        protected override void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            var credentials = clientCredentials.UserName;
            credentials.UserName = options.Value.User;
            credentials.Password = options.Value.Password;
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "It's instantiated through the dependency injection container")]
    private class B2BServiceAsyncOnlyConfiguration(SecretClient secretClient, IOptions<B2BServiceOptions> options) : B2BServiceConfiguration(options)
    {
        private readonly IOptions<B2BServiceOptions> _options = options;

        protected override async Task ConfigureEndpointAsync(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
        {
            KeyVaultSecret secret = await secretClient.GetSecretAsync("B2BService");
            var escapedCredentials = secret.Value.Split(':');
            var user = Uri.UnescapeDataString(escapedCredentials[0]);
            var password = Uri.UnescapeDataString(escapedCredentials[1]);

            var credentials = clientCredentials.UserName;
            credentials.UserName = user;
            credentials.Password = password;

            _options.Value.BillerId = Uri.UnescapeDataString(escapedCredentials[2]);
        }
    }
}