namespace Wcf.HttpClientFactory;

internal sealed class CredentialsTracker : ClientCredentials
{
    private ClientCredentialsSecurityTokenManager? _securityTokenManager;

    public CredentialsTracker()
    {
    }

    private CredentialsTracker(CredentialsTracker credentials) : base(credentials)
    {
    }

    public override SecurityTokenManager CreateSecurityTokenManager()
    {
        _securityTokenManager = (ClientCredentialsSecurityTokenManager)base.CreateSecurityTokenManager();
        return _securityTokenManager;
    }

    protected override ClientCredentials CloneCore() => new CredentialsTracker(this);

    public ClientCredentials Credentials => _securityTokenManager?.ClientCredentials ?? this;
}