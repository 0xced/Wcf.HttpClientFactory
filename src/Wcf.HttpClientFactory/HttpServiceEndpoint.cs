namespace Wcf.HttpClientFactory;

internal sealed class HttpServiceEndpoint : ServiceEndpoint
{
    public string HttpClientName { get; }

    public HttpServiceEndpoint(string httpClientName, ContractDescription contract, Binding binding, EndpointAddress address) : base(contract, binding, address)
    {
        HttpClientName = httpClientName;
    }
}