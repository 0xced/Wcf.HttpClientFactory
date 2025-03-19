using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;

[assembly: ExcludeFromCodeCoverage]

// ReSharper disable once CheckNamespace
namespace ServiceReference;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by the dependency injection container")]
public partial class B2BServiceClient
{
    public static Binding DefaultBinding => GetDefaultBinding();

    public static EndpointAddress DefaultEndpointAddress => GetDefaultEndpointAddress();
}