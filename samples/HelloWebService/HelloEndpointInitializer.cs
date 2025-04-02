using System.ServiceModel;
using LearnWebServices;

namespace HelloWebService;

public class HelloEndpointInitializer(ChannelFactory<HelloEndpoint> channelFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Factory.FromAsync(channelFactory.BeginOpen, channelFactory.EndOpen, state: null);
    }
}