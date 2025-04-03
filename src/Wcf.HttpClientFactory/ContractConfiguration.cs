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
public abstract class ContractConfiguration<TContract> : ContractConfiguration
    where TContract : class
{
    private readonly Lazy<bool> _isConfigureEndpointAsyncOverridden;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractConfiguration{TContract}"/> class.
    /// </summary>
    protected ContractConfiguration() => _isConfigureEndpointAsyncOverridden = new Lazy<bool>(IsConfigureEndpointAsyncOverridden);

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

    internal ServiceLifetime FactoryLifetime { get; set; }

    /// <summary>
    /// Optionally override this method to configure the <see cref="ServiceEndpoint"/> and/or the <see cref="ClientCredentials"/> used for connecting to the service.
    /// </summary>
    /// <param name="endpoint">The <see cref="ServiceEndpoint"/> used for connecting to the service.</param>
    /// <param name="clientCredentials">The <see cref="ClientCredentials"/> used for connecting to the service.</param>
    protected internal virtual void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
    {
        if (_isConfigureEndpointAsyncOverridden.Value)
        {
            // Revisit this if "[API Proposal]: Asynchronous DI support" eventually lands into Microsoft.Extensions.DependencyInjection
            // https://github.com/dotnet/runtime/issues/65656
            var message = FactoryLifetime switch
            {
                ServiceLifetime.Transient => $"Please override the ConfigureEndpoint method in {GetType().GetFormattedName()}. " +
                                             $"Alternatively, the {typeof(ChannelFactory<TContract>).GetFormattedName()} can be registered as singleton or scoped and be opened asynchronously prior to instantiating {typeof(TContract).GetFormattedName()}.",
                _ => $"The {typeof(ChannelFactory<TContract>).GetFormattedName()} should be opened asynchronously prior to instantiating {typeof(TContract).GetFormattedName()}.",
            };
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Optionally override this method to configure asynchronously the <see cref="ServiceEndpoint"/> and/or the <see cref="ClientCredentials"/> used for connecting to the service.
    /// </summary>
    /// <param name="endpoint">The <see cref="ServiceEndpoint"/> used for connecting to the service.</param>
    /// <param name="clientCredentials">The <see cref="ClientCredentials"/> used for connecting to the service.</param>
    /// <remarks>
    /// For this method to be called instead of <see cref="ConfigureEndpoint"/>, the <see cref="ChannelFactory{TContract}"/> must be explicitly opened asynchronously.
    /// <para/>
    /// For example, in ASP.NET Core, this can be achieved at startup in a hosted service by injecting the channel factory.
    /// <code>
    /// using System.ServiceModel;
    /// using System.Threading;
    /// using System.Threading.Tasks;
    /// using LearnWebServices;
    /// using Microsoft.Extensions.Hosting;
    ///  
    /// namespace HelloWebService;
    ///  
    /// public class HelloEndpointInitializer(ChannelFactory&lt;HelloEndpoint&gt; channelFactory) : BackgroundService
    /// {
    ///     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    ///     {
    ///         await Task.Factory.FromAsync(channelFactory.BeginOpen, channelFactory.EndOpen, state: null);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    protected internal virtual Task ConfigureEndpointAsync(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
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

    // No "good" solution to check if a method is overridden, see https://github.com/dotnet/runtime/issues/111083
    private bool IsConfigureEndpointAsyncOverridden()
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