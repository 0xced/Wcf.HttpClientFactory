namespace Wcf.HttpClientFactory;

/// <summary>
/// See [Singleton WCF Client doesn't respect DNS changes][1] and [Leverage HttpClientFactory to get benefits of handlers][2] and [Question: How to assign custom HttpClient to Binding?][3]
/// [1]: https://github.com/dotnet/wcf/issues/3230
/// [3]: https://github.com/dotnet/wcf/issues/4204
/// [2]: https://github.com/dotnet/wcf/issues/4214
/// </summary>
internal class HttpMessageHandlerBehavior : IEndpointBehavior
{
    private static readonly PropertyInfo? Handler = typeof(HttpClientHandler).GetProperty("Handler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

    public HttpMessageHandlerBehavior(IServiceProvider serviceProvider, IHttpMessageHandlerFactory messageHandlerFactory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _httpMessageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
    }

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        bindingParameters.Add((Func<HttpClientHandler, HttpMessageHandler>)(clientHandler =>
        {
            var httpServiceEndpoint = (HttpServiceEndpoint)endpoint;

            var configureMessageHandler = true;
            if (Handler?.GetValue(clientHandler) is SocketsHttpHandler socketsHttpHandler)
            {
                var configuration = (ContractConfiguration)_serviceProvider.GetRequiredService(httpServiceEndpoint.ContractConfigurationType);
                configureMessageHandler = configuration.ConfigureSocketsHttpHandler(socketsHttpHandler);
            }

            if (configureMessageHandler)
            {
                var messageHandler = _httpMessageHandlerFactory.CreateHandler(httpServiceEndpoint.HttpClientName);
                SetPrimaryHttpClientHandler(messageHandler, clientHandler);
                return messageHandler;
            }

            return clientHandler;
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
