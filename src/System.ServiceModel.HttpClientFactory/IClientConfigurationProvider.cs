using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public interface IClientConfigurationProvider
{
    string GetName(ContractDescription contractDescription);
    Binding GetBinding(ContractDescription contractDescription);
    EndpointAddress GetEndpointAddress(ContractDescription contractDescription);
}

internal static class ClientConfigurationProviderExtensions
{
    public static string GetValidName(this IClientConfigurationProvider provider, ContractDescription contractDescription)
    {
        var name = provider.GetName(contractDescription);
        if (name == null) throw new InvalidOperationException($"{provider.GetType().FullName}.GetName({contractDescription}) must not return null");
        if (name.Length == 0) throw new InvalidOperationException($"{provider.GetType().FullName}.GetName({contractDescription}) must not return an empty sting");
        return name;
    }
}