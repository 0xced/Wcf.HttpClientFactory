namespace System.ServiceModel.HttpClientFactory;

internal static class ContractConfigurationExtensions
{
    public static string GetValidName(this IContractConfiguration configuration)
    {
        var name = configuration.GetName();
        if (name == null) throw new InvalidOperationException($"{configuration.GetType().FullName}.GetName() must not return null.");
        if (name.Length == 0) throw new InvalidOperationException($"{configuration.GetType().FullName}.GetName() must not return an empty sting.");
        return name;
    }
}