namespace Wcf.HttpClientFactory;

internal class ContractTypeException(string message, Type interfaceType) : Exception(message)
{
    public Type InterfaceType { get; } = interfaceType;
}