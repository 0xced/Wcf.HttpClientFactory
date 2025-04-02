namespace Wcf.HttpClientFactory;

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