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
            0 => throw new InvalidOperationException(GetMessage(contractType)),
            1 => clientTypes[0],
            _ => throw new InvalidOperationException($"Multiple ClientBase<{contractType.GetFormattedName(TypeNameFormatOptions.Namespaces)}> were found in the {assembly.GetName().Name} assembly"),
        };
    }

    private static string GetMessage(Type contractType)
    {
        var message = $"No ClientBase<{contractType.GetFormattedName(TypeNameFormatOptions.Namespaces)}> were found in the {contractType.Assembly.GetName().Name} assembly";
        var interfaces = contractType.GetInterfaces().Where(e => e.Assembly == contractType.Assembly).ToList();
        if (interfaces.Count == 1)
        {
            throw new ContractTypeException(message, interfaces[0]);
        }
        return message;
    }
}