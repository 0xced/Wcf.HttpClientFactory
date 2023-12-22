namespace Wcf.HttpClientFactory;

public abstract class ContractConfiguration
{
    protected internal virtual bool ConfigureSocketsHttpHandler(SocketsHttpHandler socketsHttpHandler) => true;
}

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "One value per closed type is what is needed as they are actually constructed from TContract")]
public class ContractConfiguration<TContract> : ContractConfiguration
    where TContract : class
{
    private static ContractDescription? _contractDescription;
    internal static ContractDescription ContractDescription
    {
        get
        {
            _contractDescription ??= ContractDescription.GetContract(typeof(TContract));
            return _contractDescription;
        }
    }

    private static Type? _clientType;
    internal static Type ClientType
    {
        get
        {
            _clientType ??= typeof(TContract).GetClientType();
            return _clientType;
        }
    }

    private static ConstructorInfo? _clientConstructor;
    private static ConstructorInfo ClientConstructor
    {
        get
        {
            if (_clientConstructor == null)
            {
                _clientConstructor = ClientType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(ServiceEndpoint) });
                if (_clientConstructor == null)
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
            }
            return _clientConstructor;
        }
    }

    protected virtual Binding GetBinding()
    {
        var getDefaultBinding = ClientType.GetMethod("GetDefaultBinding", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultBinding != null)
            return (Binding)(getDefaultBinding.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultBinding.Name} returned null"));

        var getBindingForEndpoint = ClientType.GetMethod("GetBindingForEndpoint", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getBindingForEndpoint != null)
            return (Binding)(getBindingForEndpoint.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getBindingForEndpoint.Name} returned null"));

        throw MissingMethodException("GetBindingForEndpoint");
    }

    protected virtual EndpointAddress GetEndpointAddress()
    {
        var getDefaultEndpointAddress = ClientType.GetMethod("GetDefaultEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getDefaultEndpointAddress != null)
            return (EndpointAddress)(getDefaultEndpointAddress.Invoke(null, Array.Empty<object>()) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getDefaultEndpointAddress.Name} returned null"));

        var getEndpointAddress = ClientType.GetMethod("GetEndpointAddress", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getEndpointAddress != null)
            return (EndpointAddress)(getEndpointAddress.Invoke(null, new object[] { 0 }) ?? throw new InvalidOperationException($"{ClientType.FullName}.{getEndpointAddress.Name} returned null"));

        throw MissingMethodException("GetEndpointAddress");
    }

    protected virtual void ConfigureEndpoint(ServiceEndpoint endpoint, ClientCredentials clientCredentials)
    {
    }

    private ServiceEndpoint? _serviceEndpoint;
    internal ServiceEndpoint GetServiceEndpoint<TConfiguration>(string httpClientName, HttpMessageHandlerBehavior httpMessageHandlerBehavior)
    {
        // Make sure that the ServiceEndpoint is the exact same instance for ClientBase caching to work properly, see https://github.com/dotnet/wcf/issues/5353
        if (_serviceEndpoint == null)
        {
            var binding = GetBinding();
            var endpointAddress = GetEndpointAddress();
            _serviceEndpoint = new HttpServiceEndpoint(typeof(TConfiguration), httpClientName, ContractDescription, binding, endpointAddress);
            _serviceEndpoint.EndpointBehaviors.Add(httpMessageHandlerBehavior);
        }
        return _serviceEndpoint;
    }

    internal ChannelFactory<TContract> CreateChannelFactory(ServiceEndpoint serviceEndpoint)
    {
        var channelFactory = new ChannelFactory<TContract>(serviceEndpoint);
        ConfigureEndpoint(channelFactory.Endpoint, channelFactory.Credentials);
        return channelFactory;
    }

    internal ClientBase<TContract> CreateClient(ServiceEndpoint serviceEndpoint)
    {
        var client = (ClientBase<TContract>)ClientConstructor.Invoke(new object[] { serviceEndpoint });
        if (client.ChannelFactory.State == CommunicationState.Created)
        {
            ConfigureEndpoint(client.Endpoint, client.ClientCredentials);
        }
        return client;
    }

    private static Exception MissingMethodException(string missingMethodName)
    {
        var message = $"The method {ClientType.FullName}.{missingMethodName} was not found. " +
                      $"Was {ClientType.Name} generated with the https://www.nuget.org/packages/dotnet-svcutil tool?";
        return new MissingMethodException(message);
    }
}