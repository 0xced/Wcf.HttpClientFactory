namespace Wcf.HttpClientFactory;

internal static class ClientCredentialsExtensions
{
    private static readonly FieldInfo? IsReadOnlyField = typeof(ClientCredentials).GetField("_isReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool IsMutable(this ClientCredentials clientCredentials)
    {
        // Try not to catch an InvalidOperationException by reading the private ClientCredentials._isReadOnly field first
        if (IsReadOnlyField?.GetValue(clientCredentials) is bool isReadOnly)
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