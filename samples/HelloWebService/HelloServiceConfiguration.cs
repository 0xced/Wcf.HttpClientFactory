using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using LearnWebServices;
using Microsoft.Extensions.Options;
using Wcf.HttpClientFactory;

namespace HelloWebService;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated via dependency injection")]
public class HelloServiceConfiguration(IOptions<HelloServiceOptions> options) : ContractConfiguration<HelloEndpoint>
{
    protected override Binding GetBinding()
    {
        return options.Value.Binding;
    }

    protected override EndpointAddress GetEndpointAddress()
    {
        return new EndpointAddress(options.Value.EndpointAddress);
    }
}