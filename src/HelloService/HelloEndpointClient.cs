using System.ServiceModel;
using System.ServiceModel.Channels;

// ReSharper disable once CheckNamespace
namespace ServiceReference;

public partial class HelloEndpointClient
{
    public static Binding DefaultBinding { get; } = GetDefaultBinding();

    public static EndpointAddress DefaultEndpointAddress { get; } = GetDefaultEndpointAddress();
}