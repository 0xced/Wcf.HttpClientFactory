using System.Collections.Concurrent;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

internal class ContractFactory<TContract> where TContract : class
{
    private readonly IClientConfigurationProvider _clientConfigurationProvider;
    private readonly HttpMessageHandlerBehavior _httpMessageHandlerBehavior;
    private readonly ConcurrentDictionary<string, ChannelFactory<TContract>> _channelFactories = new();

    public ContractFactory(IClientConfigurationProvider clientConfigurationProvider, HttpMessageHandlerBehavior httpMessageHandlerBehavior)
    {
        _clientConfigurationProvider = clientConfigurationProvider ?? throw new ArgumentNullException(nameof(clientConfigurationProvider));
        _httpMessageHandlerBehavior = httpMessageHandlerBehavior ?? throw new ArgumentNullException(nameof(httpMessageHandlerBehavior));
    }

    public TContract CreateContract(ContractDescription contractDescription, Type clientType)
    {
        if (AppContext.TryGetSwitch("System.ServiceModel.HttpClientFactory.CreateClientBase", out var createClientBase) && createClientBase)
        {
            return CreateClientBase(contractDescription, clientType);
        }

        return CreateChannel(contractDescription);
    }

    private TContract CreateClientBase(ContractDescription contractDescription, Type clientType)
    {
        var binding = _clientConfigurationProvider.GetBinding(contractDescription.ConfigurationName);
        var endpointAddress = _clientConfigurationProvider.GetEndpointAddress(contractDescription.ConfigurationName);
        var constructor = clientType.GetConstructor(new[] { typeof(Binding), typeof(EndpointAddress) }) ?? throw new MissingMemberException(clientType.FullName, $"{clientType.Name}(Binding, EndpointAddress)");
        var client = (ClientBase<TContract>)constructor.Invoke(new object[] { binding, endpointAddress });
        client.Endpoint.EndpointBehaviors.Add(_httpMessageHandlerBehavior);
        return client as TContract ?? throw new InvalidCastException($"Cast from {client.GetType().FullName} to {typeof(TContract).FullName} is not valid");
    }

    private TContract CreateChannel(ContractDescription contractDescription)
    {
        ChannelFactory<TContract> channelFactory;
        if (AppContext.TryGetSwitch("System.ServiceModel.HttpClientFactory.CacheChannelFactory", out var cacheChannelFactory) && cacheChannelFactory)
        {
            channelFactory = _channelFactories.GetOrAdd(contractDescription.ConfigurationName, configurationName => CreateChannelFactory(configurationName, contractDescription));
        }
        else
        {
            channelFactory = CreateChannelFactory(contractDescription.ConfigurationName, contractDescription);
        }
        return channelFactory.CreateChannel();
    }

    private ChannelFactory<TContract> CreateChannelFactory(string configurationName, ContractDescription contractDescription)
    {
        var binding = _clientConfigurationProvider.GetBinding(configurationName);
        var endpointAddress = _clientConfigurationProvider.GetEndpointAddress(configurationName);
        var endpoint = new ServiceEndpoint(contractDescription, binding, endpointAddress);
        endpoint.EndpointBehaviors.Add(_httpMessageHandlerBehavior);
        return new ChannelFactory<TContract>(endpoint);
    }
}