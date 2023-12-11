# Wcf.HttpClientFactory

My attempt at using [IHttpClientFactory][1] with the WCF client libraries (`System.ServiceModel.*` NuGet packages)

See [Singleton WCF Client doesn't respect DNS changes][2] and [Leverage HttpClientFactory to get benefits of handlers][3] and [Question: How to assign custom HttpClient to Binding?][4]

[1]: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
[2]: https://github.com/dotnet/wcf/issues/3230
[3]: https://github.com/dotnet/wcf/issues/4204
[4]: https://github.com/dotnet/wcf/issues/4214

## Generating the B2BService client

```
cd src\B2BService
dotnet svcutil https://ebill-ki.postfinance.ch/B2BService/B2BService.svc --namespace "*, ServiceReference" --targetFramework net6.0
dotnet new classlib -f net6.0
dotnet add package System.ServiceModel.Http --version 6.2.0
rm Class1.cs
```

## Generating the Hello web service client

```
cd src/HelloService
dotnet svcutil --targetFramework net6.0 --namespace "*, ServiceReference" "https://apps.learnwebservices.com/services/hello?WSDL"
dotnet new classlib -f net6.0
dotnet add package System.ServiceModel.Http --version 6.2.0
rm Class1.cs
```

## Generating the Calculator web service client

```
cd src/CalculatorService
dotnet svcutil --targetFramework net6.0 --namespace "*, ServiceReference" "http://www.dneonline.com/calculator.asmx?wsdl"
dotnet new classlib -f net6.0
dotnet add package System.ServiceModel.Http --version 6.2.0
rm Class1.cs
```

