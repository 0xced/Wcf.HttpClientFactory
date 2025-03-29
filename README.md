# Wcf.HttpClientFactory

`Wcf.HttpClientFactory` is a library for using `IHttpClientFactory` with the WCF client libraries, also known as [System.ServiceModel.Http](https://www.nuget.org/packages/System.ServiceModel.Http). [Use IHttpClientFactory to implement resilient HTTP requests](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) explains the shorcomings of `HttpClient` and why using `IHttpClientFactory` is important. In a nutshell, it avoids both the [socket exhaustion](https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/) problem and the [DNS changes issue](https://github.com/dotnet/runtime/issues/18348). More benefits are also explained in this article, don't hesitate to read it.

Since using `IHttpClientFactory` is tightly coupled to Microsoft's [dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) library, `Wcf.HttpClientFactory` has been designed as an extension method (`AddContract`) on `IServiceCollection`.

### Getting Started

This guide uses [Learn Web Services](https://www.learnwebservices.com), a free, public SOAP web service example.

The first step is to generate a C# SOAP client to access the *Hello* web service.

1. Install the `dotnet-svcutil` tool globally

```sh
dotnet tool install --global --verbosity normal dotnet-svcutil
```

2. Generate a client library project, named `HelloService`

```sh
mkdir HelloService && cd HelloService
dotnet svcutil --targetFramework net8.0 --serviceContract --namespace "*, LearnWebServices" "https://apps.learnwebservices.com/services/hello?WSDL"
dotnet new classlib -f net8.0
rm Class1.cs
dotnet add package System.ServiceModel.Http
```

This generates the `HelloEndpoint` interface (even though it doesn't start with `I` as per C# convention on interfaces naming).

3. Add the [Wcf.HttpClientFactory](https://www.nuget.org/packages/Wcf.HttpClientFactory) NuGet package to your project using the NuGet Package Manager or run the following command:

⚠️ The NuGet package is not yet published.

```sh
dotnet add package Wcf.HttpClientFactory
```

Register the `HelloEndpoint` interface in the dependency injection services collection with an associated configuration class which will be detailed below.

```csharp
using LearnWebServices; // 👈 namespace of the generated SOAP client
using Wcf.HttpClientFactory; // 👈 for the AddContract extension method to be available

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddContract<HelloEndpoint, HelloServiceConfiguration>();
```

The `AddContract` extension method accepts different parameters which all have a sensible default values. The xmldoc explains what they are in case you need to use a non default value. The `AddContract` method returns an `IHttpClientBuilder` so that delegating handlers can be configured, for example to implement resiliency with Polly or to tweak HTTP headers to workaround a non compliant HTTP server.

### Configuring the SOAP client

TODO:

* explain the configuration class, its DI benefits and its overridable methods
* explain how to inject and use `HelloEndpoint` (e.g. in a controller vs [in a background service](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service))

### References

* [Channel Factory and Caching](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/channel-factory-and-caching)
* [Guidelines for using HttpClient — DNS behavior](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior)
* [Avoid DNS issues with HttpClient in .NET](https://www.meziantou.net/avoid-dns-issues-with-httpclient-in-dotnet.htm)

Related WCF issues:

* [Singleton WCF Client doesn't respect DNS changes](https://github.com/dotnet/wcf/issues/3230) (#3230)
* [Leverage HttpClientFactory to get benefits of handlers](https://github.com/dotnet/wcf/issues/4204) (#4204)
* [Question: How to assign custom HttpClient to Binding?](https://github.com/dotnet/wcf/issues/4214) (#4214)
