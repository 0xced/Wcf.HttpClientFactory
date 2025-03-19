using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

// ReSharper disable once CheckNamespace
namespace ServiceReference;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "It is instantiated by the dependency injection container")]
public partial class HelloEndpointClient
{
    public static Uri DefaultUri => GetDefaultEndpointAddress().Uri;
}