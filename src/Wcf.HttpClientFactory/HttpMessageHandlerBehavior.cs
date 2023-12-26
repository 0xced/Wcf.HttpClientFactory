namespace Wcf.HttpClientFactory;

internal class HttpMessageHandlerBehavior<TConfiguration> : IEndpointBehavior where TConfiguration : ContractConfiguration
{
    private readonly TConfiguration _configuration;
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

    public HttpMessageHandlerBehavior(TConfiguration configuration, IHttpMessageHandlerFactory messageHandlerFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpMessageHandlerFactory = messageHandlerFactory ?? throw new ArgumentNullException(nameof(messageHandlerFactory));
    }

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
