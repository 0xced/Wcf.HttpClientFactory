namespace Wcf.HttpClientFactory;

/// <summary>
/// See [Singleton WCF Client doesn't respect DNS changes][1] and [Leverage HttpClientFactory to get benefits of handlers][2] and [Question: How to assign custom HttpClient to Binding?][3]
/// [1]: https://github.com/dotnet/wcf/issues/3230
/// [3]: https://github.com/dotnet/wcf/issues/4204
/// [2]: https://github.com/dotnet/wcf/issues/4214
/// </summary>
internal class HttpMessageHandlerBehavior : IEndpointBehavior
{
    private readonly ContractMappingRegistry _contractMappingRegistry;
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

    public HttpMessageHandlerBehavior(ContractMappingRegistry contractMappingRegistry, IHttpMessageHandlerFactory messageHandlerFactory)
    {
        _contractMappingRegistry = contractMappingRegistry ?? throw new ArgumentNullException(nameof(contractMappingRegistry));
        _httpMessageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
    }

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        bindingParameters.Add((Func<HttpClientHandler, HttpMessageHandler>)(clientHandler =>
        {
            var httpClientName = _contractMappingRegistry.GetHttpClientName(endpoint.Contract.ContractType);
            var messageHandler = _httpMessageHandlerFactory.CreateHandler(httpClientName);
            SetPrimaryHttpClientHandler(messageHandler, clientHandler);
            return messageHandler;
        }));
    }

    private static void SetPrimaryHttpClientHandler(HttpMessageHandler messageHandler, HttpMessageHandler primaryHandler)
    {
        var delegatingHandler = messageHandler as DelegatingHandler;
        do
        {
            var innerHandler = delegatingHandler?.InnerHandler as DelegatingHandler;
            if (delegatingHandler?.InnerHandler != null && innerHandler?.InnerHandler == null)
            {
                delegatingHandler.InnerHandler = primaryHandler;
            }
            delegatingHandler = innerHandler;
        } while (delegatingHandler?.InnerHandler != null);
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) {}
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) {}
    public void Validate(ServiceEndpoint endpoint) {}
}
