namespace System.ServiceModel.HttpClientFactory;

internal static class ContractConfigurationExtensions
{
    public static string GetValidHttpClientName(this ContractConfiguration configuration)
    {
        var name = configuration.GetHttpClientName();
        if (name == null) throw new InvalidOperationException($"{configuration.GetType().FullName}.GetHttpClientName() must not return null.");
        if (name.Length == 0) throw new InvalidOperationException($"{configuration.GetType().FullName}.GetHttpClientName() must not return an empty sting.");
        return name;
    }
}