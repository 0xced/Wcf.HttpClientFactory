﻿[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName=".NET 6.0")]
namespace Wcf.HttpClientFactory
{
    public abstract class ContractConfiguration
    {
        protected ContractConfiguration() { }
        protected virtual bool ConfigureSocketsHttpHandler(System.Net.Http.SocketsHttpHandler socketsHttpHandler) { }
    }
    public class ContractConfiguration<TContract> : Wcf.HttpClientFactory.ContractConfiguration
        where TContract :  class
    {
        public ContractConfiguration() { }
        protected virtual void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Description.ClientCredentials clientCredentials) { }
        protected virtual System.ServiceModel.Channels.Binding GetBinding() { }
        protected virtual System.ServiceModel.EndpointAddress GetEndpointAddress() { }
    }
    public static class ServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddContract<TContract>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string? httpClientName = null, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = 2, bool registerChannelFactory = true)
            where TContract :  class { }
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddContract<TContract, TConfiguration>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string? httpClientName = null, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = 2, bool registerChannelFactory = true)
            where TContract :  class
            where TConfiguration : Wcf.HttpClientFactory.ContractConfiguration<TContract> { }
    }
}