namespace Wcf.HttpClientFactory;

public class ContractConfiguration
{
    private Binding? _binding;
    private EndpointAddress? _endpointAddress;
    private readonly ContractDescription _contractDescription;
    private readonly ConcurrentDictionary<(ContractDescription ContractDescription, Binding Binding, EndpointAddress EndpointAddress), ServiceEndpoint> _cache = new();

    protected Type ClientType { get; }

    private protected static readonly FieldInfo? IsReadOnlyField = typeof(ClientCredentials).GetField("_isReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);

    protected ContractConfiguration(ContractDescription contractDescription)
    {
        _contractDescription = contractDescription;
        ClientType = _contractDescription.GetClientType();
    }

    internal ServiceEndpoint GetServiceEndpoint()
    {
        var binding = GetBinding();
        var endpointAddress = GetEndpointAddress();
        return _cache.GetOrAdd((_contractDescription, binding, endpointAddress), key =>
        {
            var serviceEndpoint = new ServiceEndpoint(key.ContractDescription, key.Binding, key.EndpointAddress);
            return serviceEndpoint;
        });
    }

    public virtual Binding GetBinding()
    {
        if (_binding != null)
        {
            return _binding;
        }

        var getDefaultBinding = ClientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
        {
            _binding = (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultBinding.Name} returned null"));
            return _binding;
        }

        var getBindingForEndpoint = ClientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
        {
            _binding = (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getBindingForEndpoint.Name} returned null"));
            return _binding;
        }

        throw new MissingMethodException(MissingMethodMessage(ClientType, "GetBindingForEndpoint"));
    }

    public virtual EndpointAddress GetEndpointAddress()
    {
        if (_endpointAddress != null)
        {
            return _endpointAddress;
        }

        var getDefaultEndpointAddress = ClientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
        {
            _endpointAddress = (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));
            return _endpointAddress;
        }

        var getEndpointAddress = ClientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
        {
            _endpointAddress = (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getEndpointAddress.Name} returned null"));
            return _endpointAddress;
        }

        throw new MissingMethodException(MissingMethodMessage(ClientType, "GetEndpointAddress"));
    }

    public virtual void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
    {
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

    internal static string GetHttpClientName<TConfiguration>()
    {
        var httpClientName = typeof(TConfiguration).GetCustomAttribute<HttpClientAttribute>()?.Name;
        return httpClientName ?? ContractDescription.Name;
    }

    public virtual ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint)
    {
        return new ConfigurationChannelFactory<TContract>(this, serviceEndpoint);
    }

    public virtual ClientBase<TContract> CreateClient(ServiceEndpoint serviceEndpoint)
    {
        var constructor = ClientType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(ServiceEndpoint) });
        if (constructor == null)
        {
            var namespaceLines = ClientType.Namespace == null ? "" : $"{Environment.NewLine}namespace {ClientType.Namespace};{Environment.NewLine}";
            var message = $$"""
                            The {{ClientType.Name}} class is missing a constructor taking a ServiceEndpoint parameter. Please add the following code in your project:
                            {{namespaceLines}}
                            {{(ClientType.IsPublic ? "public" : "internal")}} partial class {{ClientType.Name}}
                            {
                                public {{ClientType.Name}}(System.ServiceModel.Description.ServiceEndpoint endpoint) : base(endpoint)
                                {
                                }
                            }

                            """.ReplaceLineEndings();
            throw new MissingMemberException(message);
        }
        var client = (ClientBase<TContract>)constructor.Invoke(new object[] { serviceEndpoint });
        if (IsMutable(client.ClientCredentials))
        {
            ConfigureEndpoint(client.Endpoint, client.ClientCredentials);
        }
        return client;
    }

    private static bool IsMutable(ClientCredentials clientCredentials)
    {
        // Try not to catch an InvalidOperationException by reading the private ClientCredentials._isReadOnly field first
        if (IsReadOnlyField != null && IsReadOnlyField.GetValue(clientCredentials) is bool isReadOnly)
        {
            return !isReadOnly;
        }

        var copy = clientCredentials.Clone();
        try
        {
            copy.UserName.UserName = "";
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}