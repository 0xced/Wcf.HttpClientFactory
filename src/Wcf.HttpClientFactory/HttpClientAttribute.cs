namespace Wcf.HttpClientFactory;

[AttributeUsage(AttributeTargets.Class)]
public class HttpClientAttribute : Attribute
{
    public HttpClientAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}