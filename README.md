# Wcf.HttpClientFactory

`Wcf.HttpClientFactory` is a library for using `IHttpClientFactory` with the WCF client libraries, also known as [System.ServiceModel.Http][1]. [Use IHttpClientFactory to implement resilient HTTP requests][2] explains the shorcomings of `HttpClient` and why using `IHttpClientFactory` is important. In a nutshell, it avoids both the [socket exhaustion][3] problem and the [DNS changes issue][4]. More benefits are also explained in this article, don't hesitate to read it.

Since using `IHttpClientFactory` is tightly coupled to Microsoft's [dependency injection][5] library, `Wcf.HttpClientFactory` has been designed as extensions methods on `IServiceCollection`.

### Getting Started

This guide uses [LEARN WEB SERVICES][6], a free, public SOAP web service.

The first step is to generate a C# SOAP client to access the *Hello* web service.

1. Install the `dotnet-svcutil` tool globally

```sh
dotnet tool install --global --verbosity normal dotnet-svcutil
```

2. Generate a client library project, named `HelloService`

```sh
mkdir HelloService && cd HelloService
dotnet svcutil --targetFramework net6.0 --namespace "*, LearnWebServices" "https://apps.learnwebservices.com/services/hello?WSDL"
dotnet new classlib -f net6.0
rm Class1.cs
dotnet add package System.ServiceModel.Http --version 6.2.0
```

This generates the `HelloEndpoint` interface and its associated `HelloEndpointClient` implementation.

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

The `AddContract` extension method accepts different parameters which all have a sensible default values. The xmldoc explains what they are in case you need to use a non default value. The `AddContract` method returns an `IHttpClientBuilder` so that delegating handlers can be configured, for example to implement resiliency with Polly or to tweak HTTP headers to workaround non compliant HTTP servers.

### Configuring the SOAP client

TODO:

* explain the configuration class, its DI benefits and its overridable methods
* explain how to inject and use `HelloEndpoint` (e.g. in a controller vs [in a background service](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service))

### References

* [Channel Factory and Caching][7]
* [Guidelines for using HttpClient — DNS behavior][8]

Related WCF issues:

* [Singleton WCF Client doesn't respect DNS changes][9] (#3230)
* [Leverage HttpClientFactory to get benefits of handlers][10] (#4204)
* [Question: How to assign custom HttpClient to Binding?][11] (#4214)

[1]: https://www.nuget.org/packages/System.ServiceModel.Http
[2]: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
[3]: https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
[4]: https://github.com/dotnet/runtime/issues/18348
[5]: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
[6]: https://www.learnwebservices.com
[7]: https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/channel-factory-and-caching
[8]: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#dns-behavior
[9]: https://github.com/dotnet/wcf/issues/3230
[10]: https://github.com/dotnet/wcf/issues/4204
[11]: https://github.com/dotnet/wcf/issues/4214
