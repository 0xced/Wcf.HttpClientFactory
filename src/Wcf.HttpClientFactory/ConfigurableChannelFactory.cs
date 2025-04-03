namespace Wcf.HttpClientFactory;

/// <summary>
/// Used to call <see cref="ContractConfiguration{TContract}.ConfigureEndpoint"/> or <see cref="ContractConfiguration{TContract}.ConfigureEndpointAsync"/> when the channel is opened.
/// </summary>
/// <typeparam name="TContract">The service contract interface.</typeparam>
/// <remarks>
/// The <see cref="CredentialsTracker"/> is needed because during the <c>OnOpening</c> call, the <see cref="ClientCredentials"/>
/// are cloned and passed to the <see cref="SecurityTokenManager"/>. Those are the credentials which are then used by WCF.
/// Thus, modifying the <c>Credentials</c> property after <c>OnOpening</c> has no effect.
/// The cloned credentials can still be modified during <c>OnOpen</c> and <c>OnBeginOpen</c> since the channel factory state is still <see cref="CommunicationState.Opening"/>
/// and the credentials are not yet marked as read-only.
/// If we supported <c>ConfigureEndpoint</c> only (without the async variant) then we could simply override <c>OnOpening</c> and pass the credentials before they are cloned.
/// <code>
/// protected override void OnOpening()
/// {
///     _configuration.ConfigureEndpoint(Endpoint, Credentials);
///     base.OnOpening();
/// }
/// </code>
/// </remarks>
internal sealed class ConfigurableChannelFactory<TContract> : ChannelFactory<TContract>
    where TContract : class
{
    private readonly ContractConfiguration<TContract> _configuration;
    private readonly CredentialsTracker _tracker;

    public ConfigurableChannelFactory(ContractConfiguration<TContract> configuration, ServiceEndpoint endpoint) : base(endpoint)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _tracker = new CredentialsTracker();
        Endpoint.EndpointBehaviors.Remove(typeof(ClientCredentials));
        Endpoint.EndpointBehaviors.Add(_tracker);
    }

    protected override void OnOpen(TimeSpan timeout)
    {
        base.OnOpen(timeout);

        _configuration.ConfigureEndpoint(Endpoint, _tracker.Credentials);
    }

    protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
    {
        var baseTask = Task.Factory.FromAsync(base.OnBeginOpen, base.OnEndOpen, timeout, state);
        var configureTask = _configuration.ConfigureEndpointAsync(Endpoint, _tracker.Credentials);

        return TaskToAsyncResult.Begin(Task.WhenAll(baseTask, configureTask), callback, state);
    }

    protected override void OnEndOpen(IAsyncResult result)
    {
        TaskToAsyncResult.End(result);
    }
}