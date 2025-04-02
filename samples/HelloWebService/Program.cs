using HelloWebService;
using LearnWebServices;
using Wcf.HttpClientFactory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<HelloServiceOptions>()
    .BindConfiguration("HelloService", bind => bind.ErrorOnUnknownConfiguration = true)
    .Validate(
        options => options.EndpointAddress != null,
        "The HelloService:EndpointAddress value must be configured in the application settings.");

builder.Services.AddContract<HelloEndpoint, HelloServiceConfiguration>();

var app = builder.Build();

app.MapGet("/", async (HelloEndpoint hello, string? name) =>
{
    var result = await hello.SayHelloAsync(new SayHello(new helloRequest { Name = name ?? "World" }));
    return result.HelloResponse.Message;
});

app.MapGet("/sync", async (IContractFactory<HelloEndpoint> helloFactory, string? name) =>
{
    var hello = helloFactory.CreateContract();
    var result = await hello.SayHelloAsync(new SayHello(new helloRequest { Name = name ?? "World" }));
    return result.HelloResponse.Message;
});

app.MapGet("/async", async (IContractFactory<HelloEndpoint> helloFactory, string? name) =>
{
    var hello = await helloFactory.CreateContractAsync();
    var result = await hello.SayHelloAsync(new SayHello(new helloRequest { Name = name ?? "World" }));
    return result.HelloResponse.Message;
});

app.Run();
