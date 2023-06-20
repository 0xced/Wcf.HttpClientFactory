using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

internal class ReflectionClientConfigurationProvider : IClientConfigurationProvider
{
    public Binding GetBinding(ContractDescription contractDescription)
    {
        var clientType = contractDescription.GetClientType();
        var getDefaultBinding = clientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(MissingMethodMessage(clientType, "GetDefaultBinding"));
        return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>())
                         ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultBinding.Name} returned null"));
    }

    public EndpointAddress GetEndpointAddress(ContractDescription contractDescription)
    {
        var clientType = contractDescription.GetClientType();
        var getDefaultEndpointAddress = clientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(MissingMethodMessage(clientType, "GetDefaultEndpointAddress"));
        return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>())
                                 ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));
    }

    private static string MissingMethodMessage(Type clientType, string missingMethodName)
    {
        return $"The method {clientType.FullName}.{missingMethodName} was not found. " +
               $"Was {clientType.FullName} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
    }
}