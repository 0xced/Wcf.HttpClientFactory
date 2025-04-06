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
/// <typeparam name="TContract">The WCF service contract interface. This type must be decorated with the <see cref="ServiceContractAttribute"/>.</typeparam>
[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "One value per closed type is what is needed as they are actually constructed from TContract")]
public abstract class ContractConfiguration<TContract> : ContractConfiguration
    where TContract : class
{
    private readonly Lazy<bool> _isConfigureEndpointAsyncOverridden;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractConfiguration{TContract}"/> class.
    /// </summary>
    protected ContractConfiguration() => _isConfigureEndpointAsyncOverridden = new Lazy<bool>(IsConfigureEndpointAsyncOverriddenImpl);

    private static ContractDescription? _contractDescription;
    internal static ContractDescription ContractDescription
    {
        get
        {
            _contractDescription ??= ContractDescription.GetContract(typeof(TContract));
            return _contractDescription;
        }
    }

    /// <summary>
    /// Returns the <see cref="Binding"/> to use for connecting to the service.
    /// </summary>
    protected abstract Binding GetBinding();

    /// <summary>
    /// Returns the <see cref="EndpointAddress"/> to use for connecting to the service.
    /// </summary>
    protected abstract EndpointAddress GetEndpointAddress();

    /// <summary>
    /// Optionally override this method to configure the <see cref="ServiceEndpoint"/> and/or the <see cref="ClientCredentials"/> used for connecting to the service.
    /// </summary>
    /// <param name="endpoint">The <see cref="ServiceEndpoint"/> used for connecting to the service.</param>
    /// <param name="clientCredentials">The <see cref="ClientCredentials"/> used for connecting to the service.</param>
    protected internal virtual void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
    {
    }

    /// <summary>
    /// Optionally override this method to configure asynchronously the <see cref="ServiceEndpoint"/> and/or the <see cref="ClientCredentials"/> used for connecting to the service.
    /// </summary>
    /// <param name="endpoint">The <see cref="ServiceEndpoint"/> used for connecting to the service.</param>
    /// <param name="clientCredentials">The <see cref="ClientCredentials"/> used for connecting to the service.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
    /// <remarks>
    /// For this method to be called instead of <see cref="ConfigureEndpoint"/>, use the <see cref="IContractFactory{TContract}.CreateContractAsync"/> method of <see cref="IContractFactory{TContract}"/>.
    /// </remarks>
    protected internal virtual Task ConfigureEndpointAsync(ServiceEndpoint endpoint, ClientCredentials clientCredentials, CancellationToken cancellationToken = default)
    {
        ConfigureEndpoint(endpoint, clientCredentials);
        return Task.CompletedTask;
    }

    internal ServiceEndpoint CreateServiceEndpoint<TConfiguration>(string httpClientName, HttpMessageHandlerBehavior<TConfiguration> httpMessageHandlerBehavior) where TConfiguration : ContractConfiguration
    {
        var binding = GetBinding();
        var endpointAddress = GetEndpointAddress();
        var serviceEndpoint = new HttpServiceEndpoint(httpClientName, ContractDescription, binding, endpointAddress);
        serviceEndpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior);
        return serviceEndpoint;
    }

    internal bool IsConfigureEndpointAsyncOverridden => _isConfigureEndpointAsyncOverridden.Value;

    // No "good" solution to check if a method is overridden, see https://github.com/dotnet/runtime/issues/111083
    private bool IsConfigureEndpointAsyncOverriddenImpl()
    {
        var baseType = typeof(ContractConfiguration<TContract>);
        var baseMethod = baseType.GetMethod(nameof(ConfigureEndpointAsync), BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new MissingMethodException(baseType.GetFormattedName(), nameof(ConfigureEndpointAsync));

        var derivedType = GetType();
        var derivedMethod = derivedType.GetMethod(nameof(ConfigureEndpointAsync), BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new MissingMethodException(derivedType.GetFormattedName(), nameof(ConfigureEndpointAsync));

        var baseDefinition = baseMethod.GetBaseDefinition();
        var derivedDefinition = derivedMethod.GetBaseDefinition();
        return baseMethod.DeclaringType != derivedMethod.DeclaringType && baseDefinition == derivedDefinition;
    }
}