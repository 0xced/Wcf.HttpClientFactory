using System.Reflection;
using System.ServiceModel.Channels;

namespace System.ServiceModel.HttpClientFactory;

internal class ReflectionClientConfigurationProvider : IClientConfigurationProvider
{
    private readonly Type _clientType;
    private readonly MethodInfo _getDefaultBinding;
    private readonly MethodInfo _getDefaultEndpointAddress;

    public ReflectionClientConfigurationProvider(Type clientType)
    {
        string MissingMethodMessage(string missingMethodName)
        {
            return $"The method {clientType.FullName}.{missingMethodName} was not found. " +
                   $"Was {clientType.FullName} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
        }

        _clientType = clientType;
        _getDefaultBinding = clientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(MissingMethodMessage("GetDefaultBinding"));
        _getDefaultEndpointAddress = clientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(MissingMethodMessage("GetDefaultEndpointAddress"));
    }

    public Binding GetBinding(string configurationName)
    {
        return (Binding)(_getDefaultBinding.Invoke(null, Array.Empty<object>())
                         ?? throw new InvalidOperationException($"{_clientType.FullName}.{_getDefaultBinding.Name} returned null"));
    }

    public EndpointAddress GetEndpointAddress(string configurationName)
    {
        return (EndpointAddress)(_getDefaultEndpointAddress.Invoke(null, Array.Empty<object>())
                                 ?? throw new InvalidOperationException($"{_clientType.FullName}.{_getDefaultEndpointAddress.Name} returned null"));
    }
}