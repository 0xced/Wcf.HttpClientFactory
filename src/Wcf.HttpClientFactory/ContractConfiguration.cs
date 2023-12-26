namespace Wcf.HttpClientFactory;

/// <summary>
/// The base class of <see cref="ContractConfiguration{TContract}"/>.
/// </summary>
public abstract class ContractConfiguration
{
    /// <summary>
    /// Override this method to configure the underlying <see cref="SocketsHttpHandler"/> of the HTTP client.
    /// For example, the <see cref="SocketsHttpHandler.PooledConnectionLifetime"/> property can be changed to properly observe DNS changes.
    /// See the <a href="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior">Guidelines for using HttpClient</a> for more information.
    /// </summary>
    /// <param name="socketsHttpHandler">The <see cref="SocketsHttpHandler"/> to configure.</param>
    /// <returns>
    /// <see langword="true"/> in order to use the <see cref="HttpMessageHandler"/> provided by the registered <see cref="IHttpMessageHandlerFactory"/>;
    /// <see langword="false"/> in order to use the default <see cref="HttpClientHandler"/> provided by WCF.
    /// </returns>
    protected internal virtual bool ConfigureSocketsHttpHandler(SocketsHttpHandler socketsHttpHandler) => true;
}

/// <summary>
/// Provides configuration opportunities for the service contract of type <typeparamref name="TContract"/>.
/// <list type="bullet">
/// <item>The contract binding can be configured by overriding the <see cref="GetBinding"/> method.</item>
/// <item>The contract endpoint address can be configured by overriding the <see cref="GetEndpointAddress"/> method.</item>
/// <item>The contract service endpoint and client credentials can be configured by overriding the <see cref="ConfigureEndpoint"/> method.</item>
/// </list>
/// </summary>
/// <typeparam name="TContract">The service contract interface. This type must be decorated with the <see cref="ServiceContractAttribute"/>.</typeparam>
[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "One value per closed type is what is needed as they are actually constructed from TContract")]
public class ContractConfiguration<TContract> : ContractConfiguration
    where TContract : class
{
    private static ContractDescription? _contractDescription;
    internal static ContractDescription ContractDescription
    {
        get
        {
            _contractDescription ??= ContractDescription.GetContract(typeof(TContract));
            return _contractDescription;
        }
    }

    private static Type? _clientType;
    internal static Type ClientType
    {
        get
        {
            _clientType ??= typeof(TContract).GetClientType();
            return _clientType;
        }
    }

    private static ConstructorInfo? _clientConstructor;
    private static ConstructorInfo ClientConstructor
    {
        get
        {
            if (_clientConstructor == null)
            {
                _clientConstructor = ClientType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(ServiceEndpoint) });
                if (_clientConstructor == null)
                {
                    var namespaceLines = ClientType.Namespace == null ? "" : $"{Environment.NewLine}namespace {ClientType.Namespace};{Environment.NewLine}";
                    var message = $$"""
                                    The {{ClientType.Name}} class is missing a constructor taking a ServiceEndpoint parameter. Please add the following code in your project:
                                    {{namespaceLines}}
                                    {{(ClientType.IsPublic ? "public" : "internal")}} partial class {{ClientType.Name}}
                                    {
                                        public {{ClientType.Name}}(System.ServiceModel.Description.ServiceEndpoint endpoint) : base(endpoint)
                                        {
                                        }
                                    }

                                    """.ReplaceLineEndings();
                    throw new MissingMemberException(message);
                }
            }
            return _clientConstructor;
        }
    }

    /// <summary>
    /// Override this method to provide the <see cref="Binding"/> to use for connecting to the service.
    /// </summary>
    /// <returns>The <see cref="Binding"/> to use for connecting to the service.</returns>
    /// <remarks>
    /// The default implementation searches for either a <c>GetDefaultBinding()</c> or a <c>GetBindingForEndpoint()</c> static method on the <see cref="ClientBase{TContract}"/> implementing
    /// the service contract using reflection.
    /// For this reason, it is recommended to always override this method.
    /// </remarks>
    protected virtual Binding GetBinding()
    {
        var getDefaultBinding = ClientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
            return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultBinding.Name} returned null"));

        var getBindingForEndpoint = ClientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
            return (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getBindingForEndpoint.Name} returned null"));

        throw MissingMethodException("GetBindingForEndpoint");
    }

    /// <summary>
    /// Override this method to provide the <see cref="EndpointAddress"/> to use for connecting to the service.
    /// </summary>
    /// <returns>The <see cref="EndpointAddress"/> to use for connecting to the service.</returns>
    /// <remarks>
    /// The default implementation searches for either a <c>GetDefaultEndpointAddress()</c> or a <c>GetEndpointAddress()</c> static method on the <see cref="ClientBase{TContract}"/> implementing
    /// the service contract using reflection.
    /// For this reason, it is recommended to always override this method.
    /// </remarks>
    protected virtual EndpointAddress GetEndpointAddress()
    {
        var getDefaultEndpointAddress = ClientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
            return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));

        var getEndpointAddress = ClientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
            return (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getEndpointAddress.Name} returned null"));

        throw MissingMethodException("GetEndpointAddress");
    }

    /// <summary>
    /// Optionally override this method to configure the <see cref="ServiceEndpoint"/> and/or the <see cref="ClientCredentials"/> used for connecting to the service.
    /// </summary>
    /// <param name="endpoint">The <see cref="ServiceEndpoint"/> used for connecting to the service.</param>
    /// <param name="clientCredentials">The <see cref="ClientCredentials"/> used for connecting to the service.</param>
    protected virtual void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
    {
    }

    private ServiceEndpoint? _serviceEndpoint;
    internal ServiceEndpoint GetServiceEndpoint<TConfiguration>(string httpClientName, HttpMessageHandlerBehavior<TConfiguration> httpMessageHandlerBehavior) where TConfiguration : ContractConfiguration
    {
        // Make sure that the ServiceEndpoint is the exact same instance for ClientBase caching to work properly, see https://github.com/dotnet/wcf/issues/5353
        if (_serviceEndpoint == null)
        {
            var binding = GetBinding();
            var endpointAddress = GetEndpointAddress();
            _serviceEndpoint = new HttpServiceEndpoint(httpClientName, ContractDescription, binding, endpointAddress);
            _serviceEndpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior);
        }
        return _serviceEndpoint;
    }

    internal ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint)
    {
        var channelFactory = new ChannelFactory<TContract>(serviceEndpoint);
        ConfigureEndpoint(channelFactory.Endpoint, channelFactory.Credentials);
        return channelFactory;
    }

    internal ClientBase<TContract> CreateClient(ServiceEndpoint serviceEndpoint)
    {
        var client = (ClientBase<TContract>)ClientConstructor.Invoke(new object[] { serviceEndpoint });
        if (client.ChannelFactory.State == CommunicationState.Created)
        {
            ConfigureEndpoint(client.Endpoint, client.ClientCredentials);
        }
        return client;
    }

    private static Exception MissingMethodException(string missingMethodName)
    {
        var message = $"The method {ClientType.FullName}.{missingMethodName} was not found. " +
                      $"Was {ClientType.Name} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
        return new MissingMethodException(message);
    }
}