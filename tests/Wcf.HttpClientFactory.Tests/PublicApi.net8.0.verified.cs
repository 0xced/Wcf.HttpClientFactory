[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/0xced/Wcf.HttpClientFactory")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v8.0", FrameworkDisplayName=".NET 8.0")]
namespace Wcf.HttpClientFactory
{
    public abstract class ContractConfiguration
    {
        protected ContractConfiguration() { }
        protected virtual bool ConfigureSocketsHttpHandler(System.Net.Http.SocketsHttpHandler socketsHttpHandler) { }
    }
    public abstract class ContractConfiguration<TContract> : Wcf.HttpClientFactory.ContractConfiguration
        where TContract :  class
    {
        protected ContractConfiguration() { }
        protected virtual void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Description.ClientCredentials clientCredentials) { }
        protected virtual System.Threading.Tasks.Task ConfigureEndpointAsync(System.ServiceModel.Description.ServiceEndpoint endpoint, System.ServiceModel.Description.ClientCredentials clientCredentials, System.Threading.CancellationToken cancellationToken = default) { }
        protected abstract System.ServiceModel.Channels.Binding GetBinding();
        protected abstract System.ServiceModel.EndpointAddress GetEndpointAddress();
    }
    public interface IContractFactory<TContract>
    {
        TContract CreateContract();
        System.Threading.Tasks.Task<TContract> CreateContractAsync(System.Threading.CancellationToken cancellationToken = default);
    }
    public static class ServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IHttpClientBuilder AddContract<TContract, TConfiguration>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string? httpClientName = null, Microsoft.Extensions.DependencyInjection.ServiceLifetime contractLifetime = 2, Microsoft.Extensions.DependencyInjection.ServiceLifetime factoryLifetime = 0)
            where TContract :  class
            where TConfiguration : Wcf.HttpClientFactory.ContractConfiguration<TContract> { }
    }
}