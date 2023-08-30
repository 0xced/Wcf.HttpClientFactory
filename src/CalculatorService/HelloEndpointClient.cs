using System.ServiceModel;
using System.ServiceModel.Channels;

// ReSharper disable once CheckNamespace
namespace ServiceReference;

public partial class CalculatorSoapClient
{
    public static Binding DefaultBinding { get; } = GetBindingForEndpoint(EndpointConfiguration.CalculatorSoap);

    public static EndpointAddress DefaultEndpointAddress { get; } = GetEndpointAddress(EndpointConfiguration.CalculatorSoap);
}