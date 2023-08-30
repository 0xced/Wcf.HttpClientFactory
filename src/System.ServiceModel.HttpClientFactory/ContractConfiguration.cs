using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public class ContractConfiguration
{
    protected readonly ContractDescription ContractDescription;

    protected ContractConfiguration(Type contractType)
    {
        ContractDescription = ContractDescription.GetContract(contractType);
    }

    public virtual string GetHttpClientName()
    {
        return ContractDescription.ContractType.AssemblyQualifiedName!;
    }

    internal string GetValidHttpClientName()
    {
        var name = GetHttpClientName();
        if (name == null) throw new InvalidOperationException($"{GetType().FullName}.GetHttpClientName() must not return null.");
        if (name.Length == 0) throw new InvalidOperationException($"{GetType().FullName}.GetHttpClientName() must not return an empty sting.");
        return name;
    }

    public virtual ServiceEndpoint GetServiceEndpoint()
    {
        var binding = GetBinding();
        var address = GetEndpointAddress();
        var clientCredentials = GetClientCredentials(binding, address);
        var serviceEndpoint = new ServiceEndpoint(ContractDescription, binding, address);
        serviceEndpoint.EndpointBehaviors.Add(clientCredentials);
        return serviceEndpoint;
    }

    public virtual Binding GetBinding()
    {
        var clientType = ContractDescription.GetClientType();

        var getDefaultBinding = clientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
            return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultBinding.Name} returned null"));

        var getBindingForEndpoint = clientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
            return (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getBindingForEndpoint.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetBindingForEndpoint"));
    }

    public virtual EndpointAddress GetEndpointAddress()
    {
        var clientType = ContractDescription.GetClientType();

        var getDefaultEndpointAddress = clientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
            return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));

        var getEndpointAddress = clientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
            return (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getEndpointAddress.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetEndpointAddress"));
    }

    public virtual ClientCredentials GetClientCredentials(Binding binding, EndpointAddress address)
    {
        return new ClientCredentials();
    }

    private static string MissingMethodMessage(Type clientType, string missingMethodName)
    {
        return $"The method {clientType.FullName}.{missingMethodName} was not found. " +
               $"Was {clientType.FullName} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
    }
}

public class ContractConfiguration<TContract> : ContractConfiguration
{
    public ContractConfiguration() : base(typeof(TContract))
    {
    }

    public virtual ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint)
    {
        return new ChannelFactory<TContract>(serviceEndpoint);
    }
}