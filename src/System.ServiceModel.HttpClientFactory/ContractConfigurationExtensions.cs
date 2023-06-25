using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

internal static class ContractConfigurationExtensions
{
    public static string GetValidName(this IContractConfiguration configuration, ContractDescription contractDescription)
    {
        var name = configuration.GetName(contractDescription);
        if (name == null) throw new InvalidOperationException($"{configuration.GetType().FullName}.GetName({contractDescription}) must not return null.");
        if (name.Length == 0) throw new InvalidOperationException($"{configuration.GetType().FullName}.GetName({contractDescription}) must not return an empty sting.");
        return name;
    }
}