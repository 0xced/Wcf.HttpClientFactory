using System.ServiceModel.Channels;

namespace System.ServiceModel.HttpClientFactory;

internal class ClientFactory<TContract> where TContract : class
{
    private readonly Type _clientType;
    private readonly string _configurationName;
    private readonly IClientConfigurationProvider _clientConfigurationProvider;
    private readonly HttpMessageHandlerBehavior _httpMessageHandlerBehavior;

    public ClientFactory(Type clientType, string configurationName, IClientConfigurationProvider clientConfigurationProvider, HttpMessageHandlerBehavior httpMessageHandlerBehavior)
    {
        _clientType = clientType ?? throw new ArgumentNullException(nameof(clientType));
        _configurationName = configurationName ?? throw new ArgumentNullException(nameof(configurationName));
        _clientConfigurationProvider = clientConfigurationProvider ?? throw new ArgumentNullException(nameof(clientConfigurationProvider));
        _httpMessageHandlerBehavior = httpMessageHandlerBehavior ?? throw new ArgumentNullException(nameof(httpMessageHandlerBehavior));
    }

    public ClientBase<TContract> CreateClient()
    {
        var constructor = _clientType.GetConstructor(new[] { typeof(Binding), typeof(EndpointAddress) })!;
        var binding = _clientConfigurationProvider.GetBinding(_configurationName);
        var endpointAddress = _clientConfigurationProvider.GetEndpointAddress(_configurationName);
        var client = (ClientBase<TContract>)constructor.Invoke(new object[] { binding, endpointAddress });
        client.Endpoint.EndpointBehaviors.Add(_httpMessageHandlerBehavior);
        return client;
    }
}