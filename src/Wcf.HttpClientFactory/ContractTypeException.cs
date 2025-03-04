namespace Wcf.HttpClientFactory;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Unnecessary")]
[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Meant to be caught internally and never escape internal classes")]
internal sealed class ContractTypeException(string message, Type interfaceType) : Exception(message)
{
    public Type InterfaceType { get; } = interfaceType;
}