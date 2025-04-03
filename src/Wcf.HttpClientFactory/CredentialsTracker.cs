namespace Wcf.HttpClientFactory;

/// <summary>
/// Used to track <see cref="ClientCredentials"/> as they are cloned and passed to the <see cref="SecurityTokenManager"/>.
/// </summary>
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