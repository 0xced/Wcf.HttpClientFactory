namespace Wcf.HttpClientFactory;

/// <summary>
/// Holds extension methods to register WCF service contract interfaces into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <typeparamref name="TContract"/> and related services to the <see cref="IServiceCollection"/> and configures a named <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="httpClientName">
    /// The logical name of the <see cref="HttpClient"/> to configure.
    /// Pass <see langword="null"/> or an empty string to use the default name from the contract.
    /// </param>
    /// <param name="lifetime">The <see cref="ServiceLifetime"/> of the registered contract.</param>
    /// <param name="registerChannelFactory">
    /// Either <see langword="true"/> to register a singleton <see cref="ChannelFactory{TContract}"/> for creating the <typeparamref name="TContract"/> instances
    /// or <see langword="false"/> to use <see cref="ClientBase{TContract}"/> for creating the <typeparamref name="TContract"/> instances.
    /// </param>
    /// <typeparam name="TContract">The type of the service contract to register. This type must be decorated with the <see cref="ServiceContractAttribute"/>.</typeparam>
    /// <typeparam name="TConfiguration">
    /// The type of the contract's configuration which provides the <see cref="Binding"/>, the <see cref="EndpointAddress"/> and enables configuration of the credentials.
    /// </typeparam>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the HTTP client.</returns>
    /// <exception cref="ArgumentException">The <typeparamref name="TContract"/> has already been registered.</exception>
    /// <exception cref="InvalidOperationException">
    /// The <typeparamref name="TContract"/> is not a service contract or the <see cref="ClientBase{TContract}"/> cache setting is not properly configured.
    /// </exception>
    public static IHttpClientBuilder AddContract<TContract, TConfiguration>(
        this IServiceCollection services,
        string? httpClientName = null,
        ServiceLifetime lifetime = ServiceLifetime.Transient,
        bool registerChannelFactory = true)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        ArgumentNullException.ThrowIfNull(services);

        // This validates that TContract is a valid service contract
        var contractDescription = GetContractDescription<TContract>();

        EnsureValidCacheSetting<TContract, TConfiguration>(lifetime, registerChannelFactory);

        var contractType = typeof(TContract);
        var descriptor = services.FirstOrDefault(e => e.ServiceType == contractType);
        if (descriptor != null)
        {
            throw new ArgumentException($"The {nameof(AddContract)}<{typeof(TContract).GetFormattedName()}, {typeof(TConfiguration).GetFormattedName()}>() method must be called only once " +
                                        $"and it was already called (with a {descriptor.Lifetime} lifetime)", nameof(TContract));
        }

        services.TryAddSingleton<TConfiguration>();
        services.TryAddSingleton<HttpMessageHandlerBehavior<TConfiguration>>();

        var clientName = string.IsNullOrEmpty(httpClientName) ? contractDescription.Name : httpClientName;

        if (registerChannelFactory)
        {
            services.AddSingleton<ChannelFactory<TContract>>(sp => CreateChannelFactory<TContract, TConfiguration>(sp, clientName));
            services.Add<TContract>(sp => sp.GetRequiredService<ChannelFactory<TContract>>().CreateChannel(), lifetime);
        }
        else
        {
            services.Add<TContract>(sp => CreateClient<TContract, TConfiguration>(sp, clientName), lifetime);
        }

        return services.AddHttpClient(clientName);
    }

    private static void EnsureValidCacheSetting<TContract, TConfiguration>(ServiceLifetime lifetime, bool registerChannelFactory)
        where TContract : class
    {
        if (lifetime == ServiceLifetime.Singleton || registerChannelFactory)
            return;

        var clientType = GetClientType<TContract, TConfiguration>();
        const string cacheSettingName = nameof(ClientBase<object>.CacheSetting);
        var cacheSettingProperty = clientType.BaseType?.GetProperty(cacheSettingName, BindingFlags.Public | BindingFlags.Static)
                                   ?? throw new MissingMethodException(clientType.FullName, cacheSettingName);
        var cacheSetting = cacheSettingProperty.GetValue(null) as CacheSetting?;
        if (cacheSetting == CacheSetting.AlwaysOn)
            return;

        var message = $"""
                      When the "{nameof(registerChannelFactory)}" argument is false, the "{lifetime}" lifetime can only be used if "{clientType.Name}" cache setting is always on.
                      Either change the "{nameof(lifetime)}" to "{nameof(ServiceLifetime.Singleton)}" or, preferably, set the cache setting to always on the client with the following code:

                      {clientType.Name}.{cacheSettingName} = {nameof(CacheSetting)}.{nameof(CacheSetting.AlwaysOn)};

                      """.ReplaceLineEndings();
        throw new InvalidOperationException(message);
    }

    private static ContractDescription GetContractDescription<TContract>() where TContract : class
    {
        try
        {
            return ContractConfiguration<TContract>.ContractDescription;
        }
        catch (InvalidOperationException exception)
        {
            throw new ArgumentException(exception.Message, nameof(TContract));
        }
    }

    private static Type GetClientType<TContract, TConfiguration>() where TContract : class
    {
        try
        {
            return ContractConfiguration<TContract>.ClientType;
        }
        catch (ContractTypeException exception)
        {
            var inheritsContractConfiguration = typeof(TConfiguration) != typeof(ContractConfiguration<TContract>);
            var configurationName = inheritsContractConfiguration ? typeof(TConfiguration).GetFormattedName() : $"ContractConfiguration<{exception.InterfaceType.Name}>";
            var message = new StringBuilder($", try with AddContract<{exception.InterfaceType.Name}, {configurationName}>() instead");
            if (inheritsContractConfiguration)
            {
                message.Append(CultureInfo.InvariantCulture, $" and make {configurationName} inherit from ContractConfiguration<{exception.InterfaceType.Name}>");
            }
            throw new ArgumentException(exception.Message + message, nameof(TContract));
        }
    }

    private static TContract CreateClient<TContract, TConfiguration>(IServiceProvider serviceProvider, string httpClientName)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var (configuration, endpoint) = GetConfigurationAndServiceEndpoint<TContract, TConfiguration>(serviceProvider, httpClientName);
        var client = configuration.CreateClient(endpoint);
        return client as TContract ?? throw new InvalidCastException($"Unable to cast object of type '{client.GetType().FullName}' to type '{typeof(TContract).FullName}'.");
    }

    private static ChannelFactory<TContract> CreateChannelFactory<TContract, TConfiguration>(IServiceProvider serviceProvider, string httpClientName)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var (configuration, endpoint) = GetConfigurationAndServiceEndpoint<TContract, TConfiguration>(serviceProvider, httpClientName);
        var channelFactory = configuration.CreateChannelFactory(endpoint);
        return channelFactory;
    }

    private static (TConfiguration configuration, ServiceEndpoint endpoint) GetConfigurationAndServiceEndpoint<TContract, TConfiguration>(IServiceProvider serviceProvider, string httpClientName)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var configuration = serviceProvider.GetRequiredService<TConfiguration>();
        var httpMessageHandlerBehavior = serviceProvider.GetRequiredService<HttpMessageHandlerBehavior<TConfiguration>>();
        var endpoint = configuration.GetServiceEndpoint(httpClientName, httpMessageHandlerBehavior);
        return (configuration, endpoint);
    }

    private static void Add<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory, ServiceLifetime lifetime) where T : notnull
    {
        var descriptor = ServiceDescriptor.Describe(typeof(T), sp => implementationFactory(sp), lifetime);
        services.Add(descriptor);
    }
}