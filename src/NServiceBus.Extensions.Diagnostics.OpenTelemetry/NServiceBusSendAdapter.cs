using System;
using NServiceBus.Extensions.Diagnostics.OpenTelemetry.Implementation;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace NServiceBus.Extensions.Diagnostics.OpenTelemetry
{
    public class NServiceBusSendAdapter : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public NServiceBusSendAdapter(Tracer tracer, NServiceBusInstrumentationOptions options)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(
                new SendMessageListener("NServiceBus.Extensions.Diagnostics.OutgoingPhysicalMessage", tracer, options), null);
            _diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose()
            => _diagnosticSourceSubscriber?.Dispose();
    }
}