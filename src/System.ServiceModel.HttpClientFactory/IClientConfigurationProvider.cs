using System.ServiceModel.Channels;

namespace System.ServiceModel.HttpClientFactory;

public interface IClientConfigurationProvider
{
    Binding GetBinding(string configurationName);
    EndpointAddress GetEndpointAddress(string configurationName);
}