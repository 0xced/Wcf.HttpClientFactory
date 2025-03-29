using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;

namespace HelloWebService;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Required for binding")]
public class HelloServiceOptions
{
    public Uri? EndpointAddress { get; set; }

    public BasicHttpBinding Binding { get; } = new();
}