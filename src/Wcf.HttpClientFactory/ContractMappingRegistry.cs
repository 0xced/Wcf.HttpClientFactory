namespace Wcf.HttpClientFactory;

/// <summary>
/// Keeps the mapping between <see cref="ContractDescription"/> instances and their associated HTTP client name.
/// </summary>
internal class ContractMappingRegistry
{
    private readonly ConcurrentDictionary<ContractDescription, string> _httpClientNames = new();

    public string this[ContractDescription contractDescription]
    {
        get => _httpClientNames[contractDescription];
        set => _httpClientNames[contractDescription] = value;
    }
}