using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

// ReSharper disable once CheckNamespace
namespace ServiceReference;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by the dependency injection container")]
public partial class HelloEndpointClient
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "It is used through reflection")]
    public HelloEndpointClient(System.ServiceModel.Description.ServiceEndpoint endpoint) : base(endpoint)
    {
    }

    public static Uri DefaultUri => GetDefaultEndpointAddress().Uri;
}