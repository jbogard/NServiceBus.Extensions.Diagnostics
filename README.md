# NServiceBus.Extensions.Diagnostics

![CI](https://github.com/jbogard/NServiceBus.Extensions.Diagnostics/workflows/CI/badge.svg)
[![NuGet](https://img.shields.io/nuget/dt/NServiceBus.Extensions.Diagnostics.svg)](https://www.nuget.org/packages/NServiceBus.Extensions.Diagnostics) 
[![NuGet](https://img.shields.io/nuget/vpre/NServiceBus.Extensions.Diagnostics.svg)](https://www.nuget.org/packages/NServiceBus.Extensions.Diagnostics)
[![MyGet (dev)](https://img.shields.io/myget/jbogard-ci/v/NServiceBus.Extensions.Diagnostics.svg)](https://myget.org/gallery/jbogard-ci)

## Usage

The `NServiceBus.Extensions.Diagnostics` package extends NServiceBus to expose telemetry information via `System.Diagnostics`.

To use `NServiceBus.Extensions.Diagnostics`, simply reference the package. The `DiagnosticsFeature` is enabled by default.

The Diagnostics package exposes four different events from [behaviors](https://docs.particular.net/nservicebus/pipeline/manipulate-with-behaviors) via Diagnostics:

 - IIncomingPhysicalMessageContext
 - IIncomingLogicalMessageContext
 - IInvokeHandlerContext
 - IOutgoingLogicalMessageContext
 - IOutgoingPhysicalMessageContext
 
The Physical message variants include full Activity support. All diagnostics events pass through the corresponding [context object](https://docs.particular.net/nservicebus/pipeline/steps-stages-connectors) as its event argument.
 
This package supports NServiceBus version 7.0 and above.

### W3C traceparent and Correlation-Context support

The Diagnostics package also provides support for both the [W3C Trace Context recommendation](https://www.w3.org/TR/trace-context/) and [W3C Correlation Context June 2020 draft](https://w3c.github.io/correlation-context/).

The Trace Context supports propagates the `traceparent` and `tracecontext` headers into outgoing messages, and populates `Activity` parent ID based on incoming messages.

The Correlation Context support consumes incoming headers into `Activity.Baggage`, and propagates `Activity.Baggage` into outgoing messages.

If you would like to add additional correlation context, inside your handler you can add additional baggage:

```csharp
Activity.Current.AddBaggage("mykey", "myvalue");
```

Correlation context can then flow out to tracing and observability tools. Common usage for correlation context are user IDs, session IDs, conversation IDs, and anything you might want to search traces to triangulate specific traces.

### `ActivitySource` support

This package exposes an [`ActivitySource`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource?view=net-5.0) with a `Name` the same as the assembly, `NServiceBus.Extensions.Diagnostics`. Use this name in any [`ActivityListener`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activitylistener?view=net-5.0)-based listeners, including [`OpenTelemetry`](https://opentelemetry.io/) using the [`OpenTelemetry.Extensions.Hosting`](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/) package:

```csharp
services.AddOpenTelemetryTracing(builder => builder
    .AddSource("NServiceBus.Extensions.Diagnostics")
```

All the available [OpenTelemetry semantic tags](https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md) are set.

### Configuring

In order to limit potentially sensitive information, the message contents are not passed through to the `ActivitySource` by default. To enable this, configure the `InstrumentationOptions` setting in your `EndpointConfiguration`:

```csharp
var settings = endpointConfiguration.GetSettings();

settings.Set(new NServiceBus.Extensions.Diagnostics.InstrumentationOptions
{
    CaptureMessageBody = true
});
```

This will set a `messaging.message_payload` tag with the UTF8-decoded message body.

### Enriching Activities

To enrich an Activity in a behavior or handler, the current executing NServiceBus activity is set in a `ICurrentActivity` extension value. In a handler or behavior you may retrieve this value and modify the `Activity`:

```csharp
public Task Handle(Message message, IMessageHandlerContext context)
{
    var currentActivity = context.Extensions.Get<ICurrentActivity>();

    currentActivity.Current?.AddBaggage("cart.operation.id", message.Id.ToString());

    // rest of method
}
```

## Metrics Usage

This package also optionally supports bridging [NServiceBus.Metrics](https://docs.particular.net/monitoring/metrics/) to [`System.Diagnostics.Metrics`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics?view=net-6.0).

It exposes the existing NServiceBus metrics with a [`Meter`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter?view=net-6.0) named `NServiceBus.Extensions.Diagnostics` and corresponding [`Counter`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1?view=net-6.0) and [`Histogram`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1?view=net-6.0) instruments, using OpenTelemetry metrics instrument and attribute [semantic conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/metrics/semantic_conventions):

| NServiceBus Probe Name | Instrument Name | Instrumentation Type |
| -- | -- | -- |
|`# of msgs successfully processed / sec` | `messaging.successes` | `Counter<long>` |
|`# of msgs pulled from the input queue /sec` | `messaging.fetches`| `Counter<long>` |
|`# of msgs failures / sec` | `messaging.failures`| `Counter<long>` |
|`Critical Time` | `messaging.client_server.duration`| `Histogram<double>` |
|`Processing Time` | `messaging.server.duration`| `Histogram<double>` |
|`Retries` | `messaging.retries`| `Counter<long>` |

Enable this feature, which also enables the `NServiceBus.Metrics` feature, in your endpoint configuration:

```csharp
endpointConfiguration.EnableFeature<DiagnosticsMetricsFeature>();
```
