namespace Wcf.HttpClientFactory;

internal static class HttpClientHandlerExtensions
{
    private static Func<object?, object?>? _getUnderlyingHandler;
    private static Func<object?, object?> GetUnderlyingHandler
    {
        get
        {
            const string underlyingHandlerFieldName = "_underlyingHandler";
            const string handlerPropertyName = "Handler";

            if (_getUnderlyingHandler == null)
            {
                // The _underlyingHandler field is typed as System.Net.Http.SocketsHttpHandler (or System.Net.Http.BrowserHttpHandler when targeting browser)
                var field = typeof(HttpClientHandler).GetField(underlyingHandlerFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType.IsAssignableTo(typeof(SocketsHttpHandler)))
                {
                    _getUnderlyingHandler = field.GetValue;
                }
                else
                {
                    // The Handler property is typed as System.Net.Http.HttpMessageHandler
                    var property = typeof(HttpClientHandler).GetProperty(handlerPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property != null && property.PropertyType.IsAssignableTo(typeof(HttpMessageHandler)))
                    {
                        _getUnderlyingHandler = property.GetValue;
                    }
                }

                if (_getUnderlyingHandler == null)
                {
                    var message = $"The {typeof(HttpClientHandler).FullName} class is missing both the " +
                                  $"{underlyingHandlerFieldName} field and the {handlerPropertyName} property.";
                    throw new MissingMemberException(message);
                }
            }
            return _getUnderlyingHandler;
        }
    }

    public static SocketsHttpHandler GetSocketsHttpHandler(this HttpClientHandler clientHandler)
    {
        var socketsHttpHandler = GetUnderlyingHandler(clientHandler) ?? throw new InvalidOperationException("The SocketsHttpHandler of the HttpClientHandler can not be null.");
        return (SocketsHttpHandler)socketsHttpHandler;
    }
}