namespace Wcf.HttpClientFactory;

public class ContractConfiguration
{
    private readonly ContractDescription _contractDescription;
    private ServiceEndpoint? _serviceEndpoint;

    protected Type ClientType { get; }

    protected ContractConfiguration(ContractDescription contractDescription)
    {
        _contractDescription = contractDescription;
        ClientType = _contractDescription.GetClientType();
    }

    internal ServiceEndpoint GetServiceEndpoint(HttpMessageHandlerBehavior httpMessageHandlerBehavior)
    {
        // Make sure that the ServiceEndpoint is the exact same instance for ClientBase caching to work properly, see https://github.com/dotnet/wcf/issues/5353
        if (_serviceEndpoint == null)
        {
            var binding = GetBinding();
            var endpointAddress = GetEndpointAddress();
            _serviceEndpoint = new ServiceEndpoint(_contractDescription, binding, endpointAddress);
            _serviceEndpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior);
        }
        return _serviceEndpoint;
    }

    protected virtual Binding GetBinding()
    {
        var getDefaultBinding = ClientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
            return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultBinding.Name} returned null"));

        var getBindingForEndpoint = ClientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
            return (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getBindingForEndpoint.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(ClientType, "GetBindingForEndpoint"));
    }

    protected virtual EndpointAddress GetEndpointAddress()
    {
        var getDefaultEndpointAddress = ClientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
            return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));

        var getEndpointAddress = ClientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
            return (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getEndpointAddress.Name} returned null"));

        throw new MissingMethodException(MissingMethodMessage(ClientType, "GetEndpointAddress"));
    }

    protected virtual void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
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

    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global", Justification = "It needs to be public in order to be instantiated by the dependency injection container")]
    public ContractConfiguration() : base(ContractDescription)
    {
    }

    internal static string GetHttpClientName<TConfiguration>()
    {
        var httpClientName = typeof(TConfiguration).GetCustomAttribute<HttpClientAttribute>()?.Name;
        return httpClientName ?? ContractDescription.Name;
    }

    internal ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint)
    {
        var channelFactory = new ChannelFactory<TContract>(serviceEndpoint);
        ConfigureEndpoint(channelFactory.Endpoint, channelFactory.Credentials);
        return channelFactory;
    }

    internal ClientBase<TContract> CreateClient(ServiceEndpoint serviceEndpoint)
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
        if (client.ClientCredentials.IsMutable())
        {
            ConfigureEndpoint(client.Endpoint, client.ClientCredentials);
        }
        return client;
    }
}