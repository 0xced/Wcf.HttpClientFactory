using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace System.ServiceModel.HttpClientFactory;

/// <summary>
/// See [Singleton WCF Client doesn't respect DNS changes][1] and [Leverage HttpClientFactory to get benefits of handlers][2] and [Question: How to assign custom HttpClient to Binding?][3]
/// [1]: https://github.com/dotnet/wcf/issues/3230
/// [3]: https://github.com/dotnet/wcf/issues/4204
/// [2]: https://github.com/dotnet/wcf/issues/4214
/// </summary>
internal class HttpMessageHandlerBehavior : IEndpointBehavior
{
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;
    private readonly IContractConfiguration _contractConfiguration;

    public HttpMessageHandlerBehavior(IHttpMessageHandlerFactory messageHandlerFactory, IContractConfiguration contractConfiguration)
    {
        _httpMessageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
        _contractConfiguration = contractConfiguration ?? throw new ArgumentNullException(nameof(contractConfiguration));
    }

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        HttpMessageHandler CreateHttpMessageHandler(HttpClientHandler clientHandler)
        {
            var name = _contractConfiguration.GetValidName(endpoint.Contract);
            var messageHandler =  _httpMessageHandlerFactory.CreateHandler(name);
            SetPrimaryHttpClientHandler(messageHandler, clientHandler);
            return messageHandler;
        }
        bindingParameters.Add((Func<HttpClientHandler, HttpMessageHandler>)CreateHttpMessageHandler);
    }

    private static void SetPrimaryHttpClientHandler(HttpMessageHandler messageHandler, HttpClientHandler primaryHandler)
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
