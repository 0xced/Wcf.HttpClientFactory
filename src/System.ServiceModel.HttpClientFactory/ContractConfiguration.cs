using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public class ContractConfiguration : IContractConfiguration
{
    public virtual string GetName(ContractDescription contractDescription)
    {
        return contractDescription.ConfigurationName;
    }

    public virtual ServiceEndpoint GetServiceEndpoint(ContractDescription contractDescription)
    {
        var binding = GetBinding(contractDescription);
        var address = GetEndpointAddress(contractDescription);
        var clientCredentials = GetClientCredentials(contractDescription);
        var serviceEndpoint = new ServiceEndpoint(contractDescription, binding, address);
        serviceEndpoint.EndpointBehaviors.Add(clientCredentials);
        return serviceEndpoint;
    }

    public virtual ChannelFactory<TContract> CreateChannelFactory<TContract>(ServiceEndpoint serviceEndpoint)
    {
        return new ChannelFactory<TContract>(serviceEndpoint);
    }

    public virtual Binding GetBinding(ContractDescription contractDescription)
    {
        var clientType = contractDescription.GetClientType();

        var getDefaultBinding = clientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
            return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultBinding.Name} returned null"));

        var getBindingForEndpoint = clientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
            return (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getBindingForEndpoint.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetBindingForEndpoint"));
    }

    public virtual EndpointAddress GetEndpointAddress(ContractDescription contractDescription)
    {
        var clientType = contractDescription.GetClientType();

        var getDefaultEndpointAddress = clientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
            return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));

        var getEndpointAddress = clientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
            return (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getEndpointAddress.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetEndpointAddress"));
    }

    public virtual ClientCredentials GetClientCredentials(ContractDescription contractDescription)
    {
        return new ClientCredentials();
    }

    private static string MissingMethodMessage(Type clientType, string missingMethodName)
    {
        return $"The method {clientType.FullName}.{missingMethodName} was not found. " +
               $"Was {clientType.FullName} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
    }
}