using System.ServiceModel.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace System.ServiceModel.HttpClientFactory;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddContract<TContract>(this IServiceCollection services, ServiceLifetime contractLifetime = ServiceLifetime.Transient, ServiceLifetime? channelFactoryLifetime = ServiceLifetime.Singleton, IClientConfigurationProvider? clientConfigurationProvider = null)
        where TContract : class
    {
        var contractDescription = ContractDescription.GetContract(typeof(TContract));
        var configurationName = contractDescription.ConfigurationName;
        if (string.IsNullOrEmpty(configurationName))
        {
            throw new ArgumentException($"The contract description of {typeof(TContract).FullName} must have a non empty configuration name", nameof(TContract));
        }

        var contractType = typeof(TContract);
        var descriptor = services.FirstOrDefault(e => e.ServiceType == contractType);
        if (descriptor != null)
        {
            throw new ArgumentException($"The {nameof(AddContract)}<{typeof(TContract).Name}> method must be called only once and it was already called (with a {descriptor.Lifetime} lifetime)", nameof(TContract));
        }

        services.TryAddSingleton(clientConfigurationProvider ?? new ReflectionClientConfigurationProvider());
        services.TryAddSingleton<HttpMessageHandlerBehavior>();

        if (channelFactoryLifetime.HasValue)
        {
            services.Add<ChannelFactory<TContract>>(sp => CreateChannelFactory<TContract>(sp, contractDescription), channelFactoryLifetime.Value);
            services.Add<TContract>(sp => sp.GetRequiredService<ChannelFactory<TContract>>().CreateChannel(), contractLifetime);
        }
        else
        {
            services.TryAddSingleton(sp => new ContractFactory<TContract>(sp.GetRequiredService<IClientConfigurationProvider>(), sp.GetRequiredService<HttpMessageHandlerBehavior>()));
            services.Add<TContract>(sp => sp.GetRequiredService<ContractFactory<TContract>>().CreateContract(contractDescription), contractLifetime);
        }

        return services.AddHttpClient(configurationName);
    }

    private static ChannelFactory<TContract> CreateChannelFactory<TContract>(IServiceProvider serviceProvider, ContractDescription contractDescription)
    {
        var clientConfigurationProvider = serviceProvider.GetRequiredService<IClientConfigurationProvider>();
        var httpMessageHandlerBehavior = serviceProvider.GetRequiredService<HttpMessageHandlerBehavior>();

        var binding = clientConfigurationProvider.GetBinding(contractDescription);
        var endpointAddress = clientConfigurationProvider.GetEndpointAddress(contractDescription);
        var endpoint = new ServiceEndpoint(contractDescription, binding, endpointAddress);
        endpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior);

        return new ChannelFactory<TContract>(endpoint);
    }

    private static void Add<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory, ServiceLifetime lifetime) where T : notnull
    {
        var descriptor = ServiceDescriptor.Describe(typeof(T), sp => implementationFactory(sp), lifetime);
        services.Add(descriptor);
    }
}