using System.Diagnostics;
using System.Linq;
using System.Text;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Settings;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace NServiceBus.Extensions.Diagnostics.OpenTelemetry.Implementation
{
    internal class SendMessageListener : ListenerHandler
    {
        private readonly NServiceBusInstrumentationOptions _options;

        public SendMessageListener(string sourceName, Tracer tracer, NServiceBusInstrumentationOptions options) : base(sourceName, tracer) 
            => _options = options;

        public override void OnStartActivity(Activity activity, object payload)
        {
            if (!(payload is IOutgoingPhysicalMessageContext context))
            {
                AdapterEventSource.Log.NullPayload("SendMessageListener.OnStartActivity");
                return;
            }

            var span = StartSpanFromActivity(activity, context);

            if (span.IsRecording)
            {
                SetSpanAttributes(context, span);
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            Tracer.CurrentSpan.End();
        }

        private TelemetrySpan StartSpanFromActivity(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            context.Headers.TryGetValue(Headers.MessageIntent, out var intent);

            var routes = context.RoutingStrategies
                .Select(r => r.Apply(context.Headers))
                .Select(t =>
                {
                    switch (t)
                    {
                        case UnicastAddressTag u:
                            return u.Destination;
                        case MulticastAddressTag m:
                            return m.MessageType.Name;
                        default:
                            return null;
                    }
                })
                .ToList();

            var operationName = $"{intent ?? activity.OperationName} {string.Join(", ", routes)}";

            Tracer.StartActiveSpanFromActivity(operationName, activity, SpanKind.Producer, out var span);
            return span;
        }

        private void SetSpanAttributes(IOutgoingPhysicalMessageContext context, TelemetrySpan span)
        {
            span.SetAttribute("messaging.message_id", context.MessageId);
            span.SetAttribute("messaging.message_payload_size_bytes", context.Body.Length);

            span.ApplyContext(context.Builder.Build<ReadOnlySettings>(), context.Headers);

            if (_options.CaptureMessageBody)
            {
                span.SetAttribute("messaging.message_payload", Encoding.UTF8.GetString(context.Body));
            }
        }
    }
}