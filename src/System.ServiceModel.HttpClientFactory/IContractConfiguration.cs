using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public interface IContractConfiguration
{
    string GetName(ContractDescription contractDescription);
    ServiceEndpoint GetServiceEndpoint(ContractDescription contractDescription);
    ChannelFactory<TContract> CreateChannelFactory<TContract>(ServiceEndpoint serviceEndpoint);
}