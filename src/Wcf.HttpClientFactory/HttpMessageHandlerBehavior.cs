namespace Wcf.HttpClientFactory;

[SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class HttpMessageHandlerBehavior<TConfiguration>(TConfiguration configuration, IHttpMessageHandlerFactory messageHandlerFactory) : IEndpointBehavior
    where TConfiguration : ContractConfiguration
{
    private readonly TConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
        bindingParameters.Add((Func<HttpClientHandler, HttpMessageHandler>)(clientHandler =>
        {
            var configureMessageHandler = _configuration.ConfigureSocketsHttpHandler(clientHandler.GetSocketsHttpHandler());

            if (configureMessageHandler)
            {
                var httpServiceEndpoint = (HttpServiceEndpoint)endpoint;
                var messageHandler = _httpMessageHandlerFactory.CreateHandler(httpServiceEndpoint.HttpClientName);
                SetPrimaryHttpClientHandler(messageHandler, clientHandler);
                return messageHandler;
            }

            return clientHandler;
        }));
    }

    private static void SetPrimaryHttpClientHandler(HttpMessageHandler messageHandler, HttpClientHandler primaryHandler)
    {
        var delegatingHandler = messageHandler as DelegatingHandler;
        do
        {
            var innerHandler = delegatingHandler?.InnerHandler as DelegatingHandler;
            // "delegatingHandler?.InnerHandler is SocketsHttpHandler" is how we identify that the messageHandler was created
            // by the _httpMessageHandlerFactory and that we must change it to the primary client handler provided by WCF
            if (delegatingHandler?.InnerHandler is SocketsHttpHandler && innerHandler?.InnerHandler == null)
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
