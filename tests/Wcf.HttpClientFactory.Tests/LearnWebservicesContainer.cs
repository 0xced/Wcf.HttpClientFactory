using System;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Wcf.HttpClientFactory.Tests;

public sealed class LearnWebservicesContainer(IContainerConfiguration configuration) : DockerContainer(configuration)
{
    public Uri WebServiceUri => new($"http://localhost:{GetMappedPublicPort(8080)}/services/hello");
}