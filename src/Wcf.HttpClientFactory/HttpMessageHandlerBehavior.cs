﻿namespace Wcf.HttpClientFactory;

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
