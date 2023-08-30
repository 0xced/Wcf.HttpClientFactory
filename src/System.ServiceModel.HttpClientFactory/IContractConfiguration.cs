using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public interface IContractConfiguration
{
    string GetName();
    ServiceEndpoint GetServiceEndpoint();
}

public interface IContractConfiguration<TContract> : IContractConfiguration
{
    ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint);
}