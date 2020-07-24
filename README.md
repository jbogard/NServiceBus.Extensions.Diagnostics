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
 - IOutgoingPhysicalMessageContext
 - IOutgoingLogicalMessageContext
 
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

