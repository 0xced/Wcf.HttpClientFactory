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
        var settingCreateClientBase = Environment.GetEnvironmentVariable("System.ServiceModel.HttpClientFactory.CreateClientBase");
        if (bool.TryParse(settingCreateClientBase, out var createClientBase) && createClientBase)
        {
            return CreateClientBase(contractDescription, clientType);
        }

        var settingCacheChannelFactory = Environment.GetEnvironmentVariable("System.ServiceModel.HttpClientFactory.CacheChannelFactory");
        return CreateChannel(contractDescription, bool.TryParse(settingCacheChannelFactory, out var cacheChannelFactory) && cacheChannelFactory);
    }

    private TContract CreateClientBase(ContractDescription contractDescription, Type clientType)
    {
        var binding = _clientConfigurationProvider.GetBinding(contractDescription.ConfigurationName);
        var endpointAddress = _clientConfigurationProvider.GetEndpointAddress(contractDescription.ConfigurationName);
        var constructor = clientType.GetConstructor(new[] { typeof(Binding), typeof(EndpointAddress) })!;
        var client = (ClientBase<TContract>)constructor.Invoke(new object[] { binding, endpointAddress });
        client.Endpoint.EndpointBehaviors.Add(_httpMessageHandlerBehavior);
        return client as TContract ?? throw new InvalidCastException($"Cast from {client.GetType().FullName} to {typeof(TContract).FullName} is not valid");
    }

    private TContract CreateChannel(ContractDescription contractDescription, bool cacheChannelFactory)
    {
        ChannelFactory<TContract> channelFactory;
        if (cacheChannelFactory)
        {
            channelFactory = _channelFactories.GetOrAdd(contractDescription.ConfigurationName, configurationName =>
            {
                var endpoint = CreateServiceEndpoint(_clientConfigurationProvider, configurationName, contractDescription, _httpMessageHandlerBehavior);
                return new ChannelFactory<TContract>(endpoint);
            });
        }
        else
        {
            var endpoint = CreateServiceEndpoint(_clientConfigurationProvider, contractDescription.ConfigurationName, contractDescription, _httpMessageHandlerBehavior);
            channelFactory = new ChannelFactory<TContract>(endpoint);
        }
        return channelFactory.CreateChannel();
    }

    private static ServiceEndpoint CreateServiceEndpoint(IClientConfigurationProvider clientConfigurationProvider, string configurationName, ContractDescription contractDescription, IEndpointBehavior behavior)
    {
        var binding = clientConfigurationProvider.GetBinding(configurationName);
        var endpointAddress = clientConfigurationProvider.GetEndpointAddress(configurationName);
        var endpoint = new ServiceEndpoint(contractDescription, binding, endpointAddress);
        endpoint.EndpointBehaviors.Add(behavior);
        return endpoint;
    }
}