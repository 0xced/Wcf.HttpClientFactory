namespace Wcf.HttpClientFactory;

internal sealed class HttpServiceEndpoint(string httpClientName, ContractDescription contract, Binding binding, EndpointAddress address)
    : ServiceEndpoint(contract, binding, address)
{
    public string HttpClientName { get; } = httpClientName;
}