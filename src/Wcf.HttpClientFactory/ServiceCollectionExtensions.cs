namespace Wcf.HttpClientFactory;

public static class ServiceCollectionExtensions
{
    private static readonly ContractMappingRegistry ContractRegistry = new();

    public static IHttpClientBuilder AddContract<TContract>(
        this IServiceCollection services,
        ServiceLifetime contractLifetime = ServiceLifetime.Transient,
        ServiceLifetime? channelFactoryLifetime = null)
        where TContract : class
    {
        return AddContract<TContract, ContractConfiguration<TContract>>(services, contractLifetime, channelFactoryLifetime);
    }

    public static IHttpClientBuilder AddContract<TContract, TConfiguration>(this IServiceCollection services,
        ServiceLifetime contractLifetime = ServiceLifetime.Transient,
        ServiceLifetime? channelFactoryLifetime = null)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var httpClientName = ContractConfiguration<TContract>.GetHttpClientName<TConfiguration>();
        if (httpClientName == null) throw new ArgumentException($"The HTTP client name of {typeof(TContract).FullName} must not be null.", nameof(TContract));
        if (httpClientName.Length == 0) throw new ArgumentException($"The HTTP client name of {typeof(TContract).FullName} must not be an empty sting.", nameof(TContract));

        EnsureValidCacheSetting<TContract>(contractLifetime, channelFactoryLifetime);

        var contractType = typeof(TContract);
        var descriptor = services.FirstOrDefault(e => e.ServiceType == contractType);
        if (descriptor != null)
        {
            throw new ArgumentException($"The {nameof(AddContract)}<{typeof(TContract).Name}> method must be called only once and it was already called (with a {descriptor.Lifetime} lifetime)", nameof(TContract));
        }

        ContractRegistry.Add<TContract>(httpClientName);

        services.TryAddSingleton(ContractRegistry);
        services.TryAddSingleton<TConfiguration>();
        services.TryAddSingleton<HttpMessageHandlerBehavior>();

        if (channelFactoryLifetime.HasValue)
        {
            services.Add<ChannelFactory<TContract>>(CreateChannelFactory<TContract, TConfiguration>, channelFactoryLifetime.Value);
            services.Add<TContract>(sp => sp.GetRequiredService<ChannelFactory<TContract>>().CreateChannel(), contractLifetime);
        }
        else
        {
            services.Add<TContract>(CreateClient<TContract, TConfiguration>, contractLifetime);
        }

        return services.AddHttpClient(httpClientName);
    }

    private static void EnsureValidCacheSetting<TContract>(ServiceLifetime contractLifetime, ServiceLifetime? channelFactoryLifetime)
        where TContract : class
    {
        if (contractLifetime != ServiceLifetime.Transient || channelFactoryLifetime != null)
            return;

        var clientType = ContractConfiguration<TContract>.ContractDescription.GetClientType();
        const string cacheSettingName = nameof(ClientBase<TContract>.CacheSetting);
        var cacheSettingProperty = clientType.BaseType?.GetProperty(cacheSettingName, BindingFlags.Public | BindingFlags.Static)
                                   ?? throw new MissingMethodException(clientType.FullName, cacheSettingName);
        var cacheSetting = cacheSettingProperty.GetValue(null) as CacheSetting?;
        if (cacheSetting == CacheSetting.AlwaysOn)
            return;

        var message = $"""
                      The {nameof(ServiceLifetime.Transient)} contract lifetime can only be used if "{clientType.Name}" cache setting is always on.
                      Either change the "{nameof(contractLifetime)}" to something else than {nameof(ServiceLifetime.Transient)} or set the cache setting to always on the client with the following code:
                      
                      {clientType.Name}.{cacheSettingName} = {nameof(CacheSetting)}.{nameof(CacheSetting.AlwaysOn)};

                      """.ReplaceLineEndings();
        throw new InvalidOperationException(message);
    }

    private static void EnsureValidCacheSetting<TContract>()
        where TContract : class
    {
    }

    private static TContract CreateClient<TContract, TConfiguration>(IServiceProvider serviceProvider)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var (configuration, endpoint) = GetConfigurationAndServiceEndpoint<TContract, TConfiguration>(serviceProvider);
        var client = configuration.CreateClient(endpoint);
        return client as TContract ?? throw new InvalidCastException($"Unable to cast object of type '{client.GetType().FullName}' to type '{typeof(TContract).FullName}'.");
    }

    private static ChannelFactory<TContract> CreateChannelFactory<TContract, TConfiguration>(IServiceProvider serviceProvider)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var (configuration, endpoint) = GetConfigurationAndServiceEndpoint<TContract, TConfiguration>(serviceProvider);
        var channelFactory = configuration.CreateChannelFactory(endpoint);
        return channelFactory;
    }

    private static (TConfiguration configuration, ServiceEndpoint endpoint) GetConfigurationAndServiceEndpoint<TContract, TConfiguration>(IServiceProvider serviceProvider)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var configuration = serviceProvider.GetRequiredService<TConfiguration>();
        var httpMessageHandlerBehavior = serviceProvider.GetRequiredService<HttpMessageHandlerBehavior>();

        var endpoint = configuration.GetServiceEndpoint();
        if (!endpoint.EndpointBehaviors.Contains(typeof(HttpMessageHandlerBehavior)))
        { 
            endpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior); 
        } 

        return (configuration, endpoint);
    }

    private static void Add<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory, ServiceLifetime lifetime) where T : notnull
    {
        var descriptor = ServiceDescriptor.Describe(typeof(T), sp => implementationFactory(sp), lifetime);
        services.Add(descriptor);
    }
}