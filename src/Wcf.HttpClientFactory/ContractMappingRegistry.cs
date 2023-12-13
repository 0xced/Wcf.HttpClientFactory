namespace Wcf.HttpClientFactory;

internal class ContractMappingRegistry
{
    private readonly Dictionary<Type, string> _contractConfigurations = new();

    public string GetHttpClientName(Type contractType) => _contractConfigurations[contractType];

    public void Add<TContract>(string httpClientName) => _contractConfigurations[typeof(TContract)] = httpClientName;
}