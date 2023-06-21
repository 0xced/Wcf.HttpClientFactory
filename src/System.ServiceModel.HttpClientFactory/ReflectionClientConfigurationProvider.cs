using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

internal class ReflectionClientConfigurationProvider : IClientConfigurationProvider
{
    public string GetName(ContractDescription contractDescription)
    {
        return contractDescription.ConfigurationName;
    }

    public Binding GetBinding(ContractDescription contractDescription)
    {
        var clientType = contractDescription.GetClientType();

        var getDefaultBinding = clientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
            return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultBinding.Name} returned null"));

        var getBindingForEndpoint = clientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
            return (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getBindingForEndpoint.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetBindingForEndpoint"));
    }

    public EndpointAddress GetEndpointAddress(ContractDescription contractDescription)
    {
        var clientType = contractDescription.GetClientType();

        var getDefaultEndpointAddress = clientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
            return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));

        var getEndpointAddress = clientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
            return (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getEndpointAddress.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetEndpointAddress"));
    }

    private static string MissingMethodMessage(Type clientType, string missingMethodName)
    {
        return $"The method {clientType.FullName}.{missingMethodName} was not found. " +
               $"Was {clientType.FullName} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
    }
}