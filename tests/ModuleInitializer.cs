using System.Runtime.CompilerServices;
using System.ServiceModel;
using ServiceReference;

namespace Wcf.HttpClientFactory.Tests;

public class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        B2BServiceClient.CacheSetting = CacheSetting.AlwaysOn;
        CalculatorSoapClient.CacheSetting = CacheSetting.AlwaysOn;
        HelloEndpointClient.CacheSetting = CacheSetting.AlwaysOn;
    }
}