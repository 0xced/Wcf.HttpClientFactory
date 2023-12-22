namespace Wcf.HttpClientFactory;

internal static class ContractTypeExtensions
{
    public static Type GetClientType(this Type contractType)
    {
        var clientBaseType = typeof(ClientBase<>).MakeGenericType(contractType);
        var assembly = contractType.Assembly;
        var clientTypes = assembly.GetTypes().Where(e => e.IsAssignableTo(contractType) && e.IsAssignableTo(clientBaseType)).ToList();
        return clientTypes.Count switch
        {
            0 => throw new InvalidOperationException($"No ClientBase<{contractType.FullName}> implementing the {contractType.FullName} contract were found in {assembly}"),
            1 => clientTypes[0],
            _ => throw new InvalidOperationException($"Multiple ClientBase<{contractType.FullName}> implementing the {contractType.FullName} contract were found in {assembly}"),
        };
    }
}