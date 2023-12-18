namespace Wcf.HttpClientFactory;

internal sealed class HttpServiceEndpoint : ServiceEndpoint
{
    public Type ContractConfigurationType { get; }
    public string HttpClientName { get; }

    public HttpServiceEndpoint(Type contractConfigurationType, string httpClientName, ContractDescription contract, Binding binding, EndpointAddress address) : base(contract, binding, address)
    {
        ContractConfigurationType = contractConfigurationType;
        HttpClientName = httpClientName;
    }
}