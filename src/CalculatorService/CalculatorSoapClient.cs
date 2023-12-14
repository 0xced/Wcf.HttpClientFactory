using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace ServiceReference;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by the dependency injection container")]
public partial class CalculatorSoapClient
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "It is used through reflection")]
    public CalculatorSoapClient(System.ServiceModel.Description.ServiceEndpoint endpoint) : base(endpoint)
    {
    }
}