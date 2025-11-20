using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
    public async Task B2BService_AsyncOnlyConfiguration_Success(ServiceLifetime factoryLifetime)
    {
        var keyVaultName = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_NAME");
        Assert.SkipWhen(string.IsNullOrEmpty(keyVaultName), "The AZURE_KEY_VAULT_NAME environment variable must be configured");

        var services = new ServiceCollection();
        services.AddLogging(c => c.AddXUnit(outputHelper));
        services.AddSingleton(new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net"), new DefaultAzureCredential()));
        services.AddContract<B2BService, B2BServiceAsyncOnlyConfiguration>(factoryLifetime: factoryLifetime);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var options = serviceProvider.GetRequiredService<IOptions<B2BServiceOptions>>().Value;

        for (var i = 1; i <= 2; i++)
        {
            var contractFactory = scope.ServiceProvider.GetRequiredService<IContractFactory<B2BService>>();
            var service = await contractFactory.CreateContractAsync(TestContext.Current.CancellationToken);
            var response = await service.ExecutePingAsync(BillerID: options.BillerId, eBillAccountID: "", ErrorTest: false, ExceptionTest: false);
            response.Should().Be(options.BillerId);
        }
    }

    [Fact]
    public async Task B2BServiceInjected_AsyncOnlyConfiguration_Error()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SecretClient(new Uri("about:blank"), new DefaultAzureCredential()));
        services.AddContract<B2BService, B2BServiceAsyncOnlyConfiguration>();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        // ReSharper disable once AccessToDisposedClosure
        var action = () => _ = scope.ServiceProvider.GetRequiredService<B2BService>();

        const string expectedMessage =
            "B2BService can not be injected directly. Instead, IContractFactory<B2BService> must be injected and CreateContractAsync() must be used to create B2BService instances. " +
            "Alternatively, B2BServiceTest.B2BServiceAsyncOnlyConfiguration.ConfigureEndpoint() can be overridden to create B2BService instances synchronously.";
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage(expectedMessage);
    }

    [Fact]
    public async Task B2BServiceCreateContract_AsyncOnlyConfiguration_Error()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SecretClient(new Uri("about:blank"), new DefaultAzureCredential()));
        services.AddContract<B2BService, B2BServiceAsyncOnlyConfiguration>();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var contractFactory = scope.ServiceProvider.GetRequiredService<IContractFactory<B2BService>>();

        // ReSharper disable once AccessToDisposedClosure
        var action = void () => _ = contractFactory.CreateContract();

        const string expectedMessage =
            "IContractFactory<B2BService>.CreateContractAsync() must be used instead of CreateContract() to create B2BService instances. " +
            "Alternatively, B2BServiceTest.B2BServiceAsyncOnlyConfiguration.ConfigureEndpoint() can be overridden to create B2BService instances synchronously.";
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
            // DisposeAsync works fine, even when in a faulted state thanks to https://github.com/dotnet/wcf/pull/4865 and https://github.com/dotnet/wcf/pull/5385
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

        protected override async Task ConfigureEndpointAsync(ServiceEndpoint endpoint, ClientCredentials clientCredentials, CancellationToken cancellationToken)
        {
            KeyVaultSecret secret = await secretClient.GetSecretAsync("B2BService", cancellationToken: cancellationToken);
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