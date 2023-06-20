using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public interface IClientConfigurationProvider
{
    Binding GetBinding(ContractDescription contractDescription);
    EndpointAddress GetEndpointAddress(ContractDescription contractDescription);
}