using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;

[assembly: ExcludeFromCodeCoverage]

// ReSharper disable once CheckNamespace
namespace ServiceReference;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by the dependency injection container")]
public partial class CalculatorSoapClient
{
    public static Binding DefaultBinding => GetBindingForEndpoint(EndpointConfiguration.CalculatorSoap);

    public static EndpointAddress DefaultEndpointAddress => GetEndpointAddress(EndpointConfiguration.CalculatorSoap);
}