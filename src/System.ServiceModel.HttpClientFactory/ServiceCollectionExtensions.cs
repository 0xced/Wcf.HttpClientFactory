using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace System.ServiceModel.HttpClientFactory;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddContract<TContract>(this IServiceCollection services, ServiceLifetime clientLifetime = ServiceLifetime.Transient, IClientConfigurationProvider? clientConfigurationProvider = null)
        where TContract : class
    {
        var contractType = typeof(TContract);

        var descriptor = services.FirstOrDefault(e => e.ServiceType == contractType);
        if (descriptor != null)
        {
            throw new InvalidOperationException($"The {nameof(AddContract)}<{typeof(TContract).Name}> method must be called only once and it was already called (with a {descriptor.Lifetime} client lifetime)");
        }

        var configurationName = contractType.GetCustomAttribute<ServiceContractAttribute>()?.ConfigurationName;
        if (string.IsNullOrEmpty(configurationName))
        {
            throw new ArgumentException($"The contract type must have a {nameof(ServiceContractAttribute)} with a non empty {nameof(ServiceContractAttribute.ConfigurationName)}", nameof(contractType));
        }
        var clientTypes = contractType.Assembly.GetExportedTypes().Where(e => e.IsAssignableTo(contractType) && e.IsAssignableTo(typeof(ClientBase<TContract>)));
        var clientType = clientTypes.Single();

        services.TryAddSingleton(clientConfigurationProvider ?? new ReflectionClientConfigurationProvider(clientType));
        services.TryAddSingleton<HttpMessageHandlerBehavior>();
        services.TryAddSingleton(sp => new ClientFactory<TContract>(clientType, configurationName, sp.GetRequiredService<IClientConfigurationProvider>(), sp.GetRequiredService<HttpMessageHandlerBehavior>()));

        if (clientLifetime == ServiceLifetime.Singleton)
        {
            services.AddSingleton(contractType, sp => sp.GetRequiredService<ClientFactory<TContract>>().CreateClient());
        }
        else if (clientLifetime == ServiceLifetime.Scoped)
        {
            services.AddScoped(contractType, sp => sp.GetRequiredService<ClientFactory<TContract>>().CreateClient());
        }
        else if (clientLifetime == ServiceLifetime.Transient)
        {
            services.AddTransient(contractType, sp => sp.GetRequiredService<ClientFactory<TContract>>().CreateClient());
        }

        return services.AddHttpClient(configurationName);
    }
}