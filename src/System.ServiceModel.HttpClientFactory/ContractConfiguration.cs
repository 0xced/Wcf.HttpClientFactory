using System.Collections.Concurrent;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace System.ServiceModel.HttpClientFactory;

public class ContractConfiguration
{
    private Binding? _binding;
    private EndpointAddress? _endpointAddress;
    private ClientCredentials? _clientCredentials;
    private readonly ContractDescription _contractDescription;
    private readonly ConcurrentDictionary<(ContractDescription ContractDescription, Binding Binding, EndpointAddress EndpointAddress, ClientCredentials ClientCredentials), ServiceEndpoint> _cache = new();

    protected ContractConfiguration(ContractDescription contractDescription)
    {
        _contractDescription = contractDescription;
    }

    internal static string GetHttpClientName<TContract, TConfiguration>()
    {
        var httpClientName = typeof(TConfiguration).GetCustomAttribute<HttpClientAttribute>()?.Name;
        return httpClientName ?? ContractDescription.GetContract(typeof(TContract)).Name;
    }

    public virtual ServiceEndpoint GetServiceEndpoint()
    {
        var binding = GetBinding();
        var endpointAddress = GetEndpointAddress();
        var clientCredentials = GetClientCredentials(binding, endpointAddress);
        return _cache.GetOrAdd((_contractDescription, binding, endpointAddress, clientCredentials), key =>
        {
            var serviceEndpoint = new ServiceEndpoint(key.ContractDescription, key.Binding, key.EndpointAddress);
            serviceEndpoint.EndpointBehaviors.Add(key.ClientCredentials);
            return serviceEndpoint;
        });
    }

    public virtual Binding GetBinding()
    {
        if (_binding != null)
        {
            return _binding;
        }

        var clientType = _contractDescription.GetClientType();

        var getDefaultBinding = clientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
        {
            _binding = (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultBinding.Name} returned null"));
            return _binding;
        }

        var getBindingForEndpoint = clientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
        {
            _binding = (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getBindingForEndpoint.Name} returned null"));
            return _binding;
        }

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetBindingForEndpoint"));
    }

    public virtual EndpointAddress GetEndpointAddress()
    {
        if (_endpointAddress != null)
        {
            return _endpointAddress;
        }

        var clientType = _contractDescription.GetClientType();

        var getDefaultEndpointAddress = clientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
        {
            _endpointAddress = (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{clientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));
            return _endpointAddress;
        }

        var getEndpointAddress = clientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
        {
            _endpointAddress = (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{clientType.FullName}.{getEndpointAddress.Name} returned null"));
            return _endpointAddress;
        }

        throw new MissingMethodException(MissingMethodMessage(clientType, "GetEndpointAddress"));
    }

    public virtual ClientCredentials GetClientCredentials(Binding binding, EndpointAddress address)
    {
        _clientCredentials ??= new ClientCredentials();
        return _clientCredentials;
    }

    private static string MissingMethodMessage(Type clientType, string missingMethodName)
    {
        return $"The method {clientType.FullName}.{missingMethodName} was not found. " +
               $"Was {clientType.FullName} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
    }
}

public class ContractConfiguration<TContract> : ContractConfiguration
    where TContract : class
{
    internal static ContractDescription ContractDescription { get; } = ContractDescription.GetContract(typeof(TContract));

    public ContractConfiguration() : base(ContractDescription)
    {
    }

    public virtual ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint)
    {
        return new ChannelFactory<TContract>(serviceEndpoint);
    }

    public virtual ClientBase<TContract> CreateClient(ServiceEndpoint serviceEndpoint)
    {
        var clientType = ContractDescription.GetClientType();
        var constructor = clientType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(ServiceEndpoint) }, null);
        if (constructor == null)
        {
            var namespaceLines = clientType.Namespace == null ? "" : $"{Environment.NewLine}namespace {clientType.Namespace};{Environment.NewLine}";
            var message = $$"""
                            The {{clientType.Name}} class is missing a constructor taking a ServiceEndpoint parameter. Please add the following code in your project:
                            {{namespaceLines}}
                            public partial class {{clientType.Name}}
                            {
                                public {{clientType.Name}}(System.ServiceModel.Description.ServiceEndpoint endpoint) : base(endpoint)
                                {
                                }
                            }

                            """.ReplaceLineEndings();
            throw new MissingMemberException(message);
        }
        var client = (ClientBase<TContract>)constructor.Invoke(new object[] { serviceEndpoint });
        return client;
    }
}