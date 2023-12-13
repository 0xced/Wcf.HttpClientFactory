using System;
using System.Diagnostics.Tracing;
using System.Linq;
using Xunit.Abstractions;

namespace Wcf.HttpClientFactory.Tests;

public class WcfEventListener : EventListener
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Guid _wcfEventSourceGuid = new("c651f5f6-1c0d-492e-8ae1-b4efd7c9d503");

    public WcfEventListener(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    protected sealed override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);

        if (eventSource.Guid == _wcfEventSourceGuid)
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var message = string.Format(eventData.Message ?? "", args: eventData.Payload?.ToArray() ?? Array.Empty<object>());
        var name = eventData.EventSource.Guid == _wcfEventSourceGuid ? "WCF" : eventData.EventSource.Name;
        _outputHelper.WriteLine($"[{name}] {message}");
    }
}