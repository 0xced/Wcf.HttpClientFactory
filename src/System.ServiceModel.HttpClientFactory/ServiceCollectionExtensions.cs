using System.ServiceModel.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace System.ServiceModel.HttpClientFactory;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddContract<TContract>(this IServiceCollection services, ServiceLifetime clientLifetime = ServiceLifetime.Transient, IClientConfigurationProvider? clientConfigurationProvider = null)
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
            throw new ArgumentException($"The {nameof(AddContract)}<{typeof(TContract).Name}> method must be called only once and it was already called (with a {descriptor.Lifetime} client lifetime)", nameof(TContract));
        }

        var clientTypes = contractType.Assembly.GetExportedTypes().Where(e => e.IsAssignableTo(contractType) && e.IsAssignableTo(typeof(ClientBase<TContract>)));
        var clientType = clientTypes.Single();

        services.TryAddSingleton(clientConfigurationProvider ?? new ReflectionClientConfigurationProvider(clientType));
        services.TryAddSingleton<HttpMessageHandlerBehavior>();
        services.TryAddSingleton(sp => new ContractFactory<TContract>(contractDescription, clientType, configurationName, sp.GetRequiredService<IClientConfigurationProvider>(), sp.GetRequiredService<HttpMessageHandlerBehavior>()));

        if (clientLifetime == ServiceLifetime.Singleton)
        {
            services.AddSingleton(sp => sp.GetRequiredService<ContractFactory<TContract>>().CreateContract());
        }
        else if (clientLifetime == ServiceLifetime.Scoped)
        {
            services.AddScoped(sp => sp.GetRequiredService<ContractFactory<TContract>>().CreateContract());
        }
        else if (clientLifetime == ServiceLifetime.Transient)
        {
            services.AddTransient(sp => sp.GetRequiredService<ContractFactory<TContract>>().CreateContract());
        }

        return services.AddHttpClient(configurationName);
    }
}