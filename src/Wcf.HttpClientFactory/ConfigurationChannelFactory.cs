namespace Wcf.HttpClientFactory;

internal class ConfigurationChannelFactory<T> : ChannelFactory<T>
    where T : class
{
    private readonly ContractConfiguration<T> _contractConfiguration;

    public ConfigurationChannelFactory(ContractConfiguration<T> contractConfiguration, ServiceEndpoint endpoint) : base(endpoint)
    {
        _contractConfiguration = contractConfiguration;
    }

    protected override void OnOpen(TimeSpan timeout)
    {
        base.OnOpen(timeout);
        _contractConfiguration.ConfigureEndpoint(Endpoint, Credentials);
    }
}