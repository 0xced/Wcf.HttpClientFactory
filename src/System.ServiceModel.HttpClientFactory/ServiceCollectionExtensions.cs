using System.Reflection;
using System.ServiceModel.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace System.ServiceModel.HttpClientFactory;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddContractHttpClientFactory<TContract>(this IServiceCollection services)
    {
        return services.AddContractHttpClientFactory(typeof(TContract));
    }

    public static IHttpClientBuilder AddContractHttpClientFactory(this IServiceCollection services, Type contractType)
    {
        services.TryAddSingleton<IEndpointBehavior, HttpMessageHandlerBehavior>();

        var configurationName = contractType.GetCustomAttribute<ServiceContractAttribute>()?.ConfigurationName;
        if (string.IsNullOrEmpty(configurationName))
        {
            throw new ArgumentException($"The contract type must have a {nameof(ServiceContractAttribute)} with a non empty {nameof(ServiceContractAttribute.ConfigurationName)}", nameof(contractType));
        }

        return services.AddHttpClient(configurationName)
            .ConfigureHttpMessageHandlerBuilder(builder =>
            {
                var httpMessageHandlerBehaviors = builder.Services.GetServices<IEndpointBehavior>().OfType<HttpMessageHandlerBehavior>().ToList();
                var httpMessageHandlerBehavior = httpMessageHandlerBehaviors.Count switch
                {
                    0 => throw new InvalidOperationException($"No {nameof(HttpMessageHandlerBehavior)} instances were found in the services"),
                    1 => httpMessageHandlerBehaviors[0],
                    _ => throw new InvalidOperationException($"Multiple {nameof(HttpMessageHandlerBehavior)} instances ({httpMessageHandlerBehaviors.Count}) were found in the services"),
                };
                var primaryHandler = httpMessageHandlerBehavior.GetHttpClientHandler(builder.Name);
                primaryHandler.Properties[HttpMessageHandlerBehavior.Sentinel] = true;
                builder.PrimaryHandler = primaryHandler;
            });
    }
}