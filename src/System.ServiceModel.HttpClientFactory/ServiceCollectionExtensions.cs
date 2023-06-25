using System.ServiceModel.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace System.ServiceModel.HttpClientFactory;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddContract<TContract>(this IServiceCollection services,
        ServiceLifetime contractLifetime = ServiceLifetime.Transient,
        ServiceLifetime channelFactoryLifetime = ServiceLifetime.Singleton,
        IContractConfiguration? contractConfiguration = null)
        where TContract : class
    {
        var configuration = contractConfiguration ?? new ContractConfiguration();
        var contractDescription = ContractDescription.GetContract(typeof(TContract));
        var name = configuration.GetValidName(contractDescription);

        var contractType = typeof(TContract);
        var descriptor = services.FirstOrDefault(e => e.ServiceType == contractType);
        if (descriptor != null)
        {
            throw new ArgumentException($"The {nameof(AddContract)}<{typeof(TContract).Name}> method must be called only once and it was already called (with a {descriptor.Lifetime} lifetime)", nameof(TContract));
        }

        services.TryAddSingleton(configuration);
        services.TryAddSingleton<HttpMessageHandlerBehavior>();

        services.Add<ChannelFactory<TContract>>(sp => CreateChannelFactory<TContract>(sp, contractDescription), channelFactoryLifetime);
        services.Add<TContract>(sp => sp.GetRequiredService<ChannelFactory<TContract>>().CreateChannel(), contractLifetime);

        return services.AddHttpClient(name);
    }

    private static ChannelFactory<TContract> CreateChannelFactory<TContract>(IServiceProvider serviceProvider, ContractDescription contractDescription)
    {
        var configuration = serviceProvider.GetRequiredService<IContractConfiguration>();
        var httpMessageHandlerBehavior = serviceProvider.GetRequiredService<HttpMessageHandlerBehavior>();

        var endpoint = configuration.GetServiceEndpoint(contractDescription);
        endpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior);

        var channelFactory = configuration.CreateChannelFactory<TContract>(endpoint);
        return channelFactory;
    }

    private static void Add<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory, ServiceLifetime lifetime) where T : notnull
    {
        var descriptor = ServiceDescriptor.Describe(typeof(T), sp => implementationFactory(sp), lifetime);
        services.Add(descriptor);
    }
}