using System.Collections.Concurrent;

namespace Wcf.HttpClientFactory;

/// <summary>
/// Used to call <see cref="ContractConfiguration{TContract}.ConfigureEndpoint"/> or <see cref="ContractConfiguration{TContract}.ConfigureEndpointAsync"/> before the channel factory is opened.
/// </summary>
/// <typeparam name="TContract">The WCF service contract interface.</typeparam>
internal sealed class ContractFactory<TContract>(ContractConfiguration<TContract> configuration, ChannelFactory<TContract> channelFactory) : IContractFactory<TContract>, IAsyncDisposable
    where TContract : class
{
    private readonly ContractConfiguration<TContract> _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ChannelFactory<TContract> _channelFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
    private readonly ConcurrentBag<IAsyncDisposable> _disposables = [];

    public TContract CreateContract()
    {
        return CreateContract(ContractCreation.FactoryMethod);
    }

    internal TContract CreateContract(ContractCreation creation)
    {
        EnsureConfiguredAndOpened(creation);

        return CreateAndCollectContract();
    }

    public async Task<TContract> CreateContractAsync(CancellationToken cancellationToken)
    {
        await EnsureConfiguredAndOpenedAsync(cancellationToken).ConfigureAwait(false);

        return CreateAndCollectContract();
    }

    private TContract CreateAndCollectContract()
    {
        var contract = _channelFactory.CreateChannel();

        if (contract is IAsyncDisposable asyncDisposable)
        {
            _disposables.Add(asyncDisposable);
        }

        return contract;
    }

    private void EnsureConfiguredAndOpened(ContractCreation creation)
    {
        try
        {
            _semaphore.Wait();

            ThrowIfConfigureEndpointAsyncIsOverridden(creation);

            if (_channelFactory.State == CommunicationState.Created)
            {
                _configuration.ConfigureEndpoint(_channelFactory.Endpoint, _channelFactory.Credentials);
                _channelFactory.Open();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureConfiguredAndOpenedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (_channelFactory.State == CommunicationState.Created)
            {
                await _configuration.ConfigureEndpointAsync(_channelFactory.Endpoint, _channelFactory.Credentials, cancellationToken).ConfigureAwait(false);
                await Task.Factory.FromAsync(_channelFactory.BeginOpen, _channelFactory.EndOpen, state: null).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ThrowIfConfigureEndpointAsyncIsOverridden(ContractCreation creation)
    {
        if (_configuration.IsConfigureEndpointAsyncOverridden)
        {
            // Revisit this if "[API Proposal]: Asynchronous DI support" eventually lands into Microsoft.Extensions.DependencyInjection
            // https://github.com/dotnet/runtime/issues/65656
            var contractFactoryType = typeof(IContractFactory<TContract>).GetFormattedName();
            var contractType = typeof(TContract).GetFormattedName();
            var configurationType = _configuration.GetType().GetFormattedName();
            const string createContract = $"{nameof(IContractFactory<TContract>.CreateContract)}()";
            const string createContractAsync = $"{nameof(IContractFactory<TContract>.CreateContractAsync)}()";
            const string configureEndpoint = $"{nameof(_configuration.ConfigureEndpoint)}()";
            var message = creation switch
            {
                ContractCreation.DependencyInjection => $"{contractType} can not be injected directly. Instead, {contractFactoryType} must be injected and {createContractAsync} must be used to create {contractType} instances.",
                ContractCreation.FactoryMethod => $"{contractFactoryType}.{createContractAsync} must be used instead of {createContract} to create {contractType} instances.",
                _ => throw new UnreachableException(),
            };
            throw new InvalidOperationException(message + $" Alternatively, {configurationType}.{configureEndpoint} can be overridden to create {contractType} instances synchronously.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_disposables.Select(t => t.DisposeAsync().AsTask())).ConfigureAwait(false);
        await ((IAsyncDisposable)_channelFactory).DisposeAsync().ConfigureAwait(false);
        _semaphore.Dispose();
    }
}