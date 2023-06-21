using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

internal class ContractFactory<TContract> where TContract : class
{
    private readonly IClientConfigurationProvider _clientConfigurationProvider;
    private readonly HttpMessageHandlerBehavior _httpMessageHandlerBehavior;

    public ContractFactory(IClientConfigurationProvider clientConfigurationProvider, HttpMessageHandlerBehavior httpMessageHandlerBehavior)
    {
        _clientConfigurationProvider = clientConfigurationProvider ?? throw new ArgumentNullException(nameof(clientConfigurationProvider));
        _httpMessageHandlerBehavior = httpMessageHandlerBehavior ?? throw new ArgumentNullException(nameof(httpMessageHandlerBehavior));
    }

    public TContract CreateContract(ContractDescription contractDescription)
    {
        var binding = _clientConfigurationProvider.GetBinding(contractDescription);
        var endpointAddress = _clientConfigurationProvider.GetEndpointAddress(contractDescription);
        var clientType = contractDescription.GetClientType();
        var constructor = clientType.GetConstructor(new[] { typeof(Binding), typeof(EndpointAddress) }) ?? throw new MissingMemberException(clientType.FullName, $"{clientType.Name}(Binding, EndpointAddress)");
        var client = (ClientBase<TContract>)constructor.Invoke(new object[] { binding, endpointAddress });
        client.Endpoint.EndpointBehaviors.Add(_httpMessageHandlerBehavior);
        return client as TContract ?? throw new InvalidCastException($"Unable to cast object of type '{client.GetType().FullName}' to type '{typeof(TContract).FullName}'.");
    }
}