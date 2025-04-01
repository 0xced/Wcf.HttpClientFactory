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
    /// <param name="contractLifetime">The <see cref="ServiceLifetime"/> of the registered contract. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <param name="factoryLifetime">
    /// The <see cref="ServiceLifetime"/> of the <see cref="ChannelFactory{TContract}"/> used for creating the <typeparamref name="TContract"/> instances.
    /// Defaults to <see cref="ServiceLifetime.Singleton"/>.
    /// </param>
    /// <typeparam name="TContract">The type of the service contract to register. This type must be decorated with the <see cref="ServiceContractAttribute"/>.</typeparam>
    /// <typeparam name="TConfiguration">
    /// The type of the contract's configuration which provides the <see cref="Binding"/>, the <see cref="EndpointAddress"/> and enables configuration of the credentials.
    /// Registered as a singleton.
    /// </typeparam>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the HTTP client.</returns>
    /// <exception cref="ArgumentException">The <typeparamref name="TContract"/> has already been registered or is not an interface of a service contract.</exception>
    public static IHttpClientBuilder AddContract<TContract, TConfiguration>(
        this IServiceCollection services,
        string? httpClientName = null,
        ServiceLifetime contractLifetime = ServiceLifetime.Transient,
        ServiceLifetime factoryLifetime = ServiceLifetime.Singleton)
        where TContract : class
        where TConfiguration : ContractConfiguration<TContract>
    {
        var contractDescription = ValidateArguments<TContract, TConfiguration>(services);

        services.TryAddSingleton<TConfiguration>();
        services.TryAddSingleton<HttpMessageHandlerBehavior<TConfiguration>>();

        var clientName = string.IsNullOrEmpty(httpClientName) ? contractDescription.Name : httpClientName;

        services.Add<ChannelFactory<TContract>>(factoryLifetime, sp =>
        {
            var configuration = sp.GetRequiredService<TConfiguration>();
            var httpMessageHandlerBehavior = sp.GetRequiredService<HttpMessageHandlerBehavior<TConfiguration>>();
            var endpoint = configuration.CreateServiceEndpoint(clientName, httpMessageHandlerBehavior);
            var channelFactory = configuration.CreateChannelFactory(endpoint);
            return channelFactory;
        });

        services.Add<TContract>(contractLifetime, static sp =>
        {
            var channelFactory = sp.GetRequiredService<ChannelFactory<TContract>>();
            return channelFactory.CreateChannel();
        });

        return services.AddHttpClient(clientName);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Converting enum values for proper English sentences.")]
    private static ContractDescription ValidateArguments<TContract, TConfiguration>(IServiceCollection services)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!typeof(TContract).IsInterface)
        {
            throw new ArgumentException($"The contract type ({typeof(TContract).GetFormattedName()}) must be an interface type.", nameof(TContract));
        }

        // This validates that TContract is a valid service contract
        var contractDescription = GetContractDescription<TContract>();

        var contractType = typeof(TContract);
        var descriptor = services.FirstOrDefault(e => e.ServiceType == contractType);
        if (descriptor != null)
        {
            throw new ArgumentException($"The {nameof(AddContract)}<{typeof(TContract).GetFormattedName()}, {typeof(TConfiguration).GetFormattedName()}>() method must be called only once " +
                                        $"and it was already called (with a {descriptor.Lifetime.ToString().ToLowerInvariant()} lifetime).", nameof(TContract));
        }

        if (typeof(TConfiguration) == typeof(ContractConfiguration<TContract>))
        {
            throw new ArgumentException($"The configuration class ({typeof(TConfiguration).GetFormattedName()}) is abstract, it must be subclassed.", nameof(TConfiguration));
        }

        return contractDescription;
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

    private static void Add<T>(this IServiceCollection services, ServiceLifetime lifetime, Func<IServiceProvider, T> implementationFactory) where T : notnull
    {
        var descriptor = ServiceDescriptor.Describe(typeof(T), sp => implementationFactory(sp), lifetime);
        services.Add(descriptor);
    }
}