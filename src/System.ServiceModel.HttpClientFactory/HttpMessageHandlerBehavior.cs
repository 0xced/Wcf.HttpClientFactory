using System.Collections.Concurrent;
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
    internal static readonly string Sentinel = typeof(HttpMessageHandlerBehavior).FullName!;

    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;
    private readonly ConcurrentDictionary<string, HttpClientHandler> _httpClientHandlers = new();

    public HttpMessageHandlerBehavior(IHttpMessageHandlerFactory messageHandlerFactory)
        => _httpMessageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        HttpMessageHandler CreateHttpMessageHandler(HttpClientHandler clientHandler)
        {
            var handlerName = endpoint.Contract.ConfigurationName ?? throw new InvalidOperationException($"The configuration name of the contract {endpoint.Contract.ContractType.FullName} can not be null");
            _httpClientHandlers.TryAdd(handlerName, clientHandler);
            var messageHandler =  _httpMessageHandlerFactory.CreateHandler(handlerName);
            var primaryHandler = GetPrimaryHttpClientHandler(messageHandler);
            if (!primaryHandler.Properties.ContainsKey(Sentinel))
            {
                throw new InvalidOperationException($"The primary handler of the {nameof(HttpMessageHandler)} returned by the {nameof(IHttpMessageHandlerFactory)} must have a sentiel");
            }
            return messageHandler;
        }
        bindingParameters.Add((Func<HttpClientHandler, HttpMessageHandler>)CreateHttpMessageHandler);
    }

    public HttpClientHandler GetHttpClientHandler(string handlerName)
    {
        if (_httpClientHandlers.TryRemove(handlerName, out var httpClientHandler))
        {
            return httpClientHandler;
        }

        throw new InvalidOperationException($"The handler named {handlerName} must be in the {_httpClientHandlers} dictionary");
    }

    private static HttpClientHandler GetPrimaryHttpClientHandler(HttpMessageHandler messageHandler)
    {
        var delegatingHandler = messageHandler as DelegatingHandler;
        do
        {
            var innerHandler = delegatingHandler?.InnerHandler as DelegatingHandler;
            if (delegatingHandler?.InnerHandler is HttpClientHandler innerHttpClientHandler && innerHandler?.InnerHandler == null)
            {
                return innerHttpClientHandler;
            }
            delegatingHandler = innerHandler;
        } while (delegatingHandler?.InnerHandler != null);

        throw new InvalidOperationException("The primary handler was not found");
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) {}
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) {}
    public void Validate(ServiceEndpoint endpoint) {}
}
