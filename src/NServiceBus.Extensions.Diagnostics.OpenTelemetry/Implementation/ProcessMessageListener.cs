using System.Diagnostics;
using System.Text;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using OpenTelemetry.Adapter;
using OpenTelemetry.Trace;

namespace NServiceBus.Extensions.Diagnostics.OpenTelemetry.Implementation
{
    internal class ProcessMessageListener : ListenerHandler
    {
        private readonly NServiceBusInstrumentationOptions _options;

        public ProcessMessageListener(string sourceName, Tracer tracer, NServiceBusInstrumentationOptions options) : base(sourceName, tracer) 
            => _options = options;

        public override void OnStartActivity(Activity activity, object payload)
        {
            if (!(payload is IIncomingPhysicalMessageContext context))
            {
                AdapterEventSource.Log.NullPayload("ProcessMessageListener.OnStartActivity");
                return;
            }

            var settings = context.Builder.Build<ReadOnlySettings>();

            Tracer.StartActiveSpanFromActivity(settings.LogicalAddress().ToString(), activity, SpanKind.Consumer, out var span);

            if (span.IsRecording)
            {
                span.SetAttribute("messaging.message_id", context.Message.MessageId);
                span.SetAttribute("messaging.operation", "process");
                span.SetAttribute("messaging.message_payload_size_bytes", context.Message.Body.Length);

                if (_options.CaptureMessageBody)
                {
                    //span.SetAttribute("messaging.message_payload", Encoding.UTF8.GetString(context.Message.Body));
                    span.SetAttribute("messaging.message_payload", context.Message.Body);
                }

                span.ApplyContext(settings, context.MessageHeaders);
            }
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            if (Tracer.CurrentSpan.IsRecording)
            {
                Tracer.CurrentSpan.End();
            }
        }
    }
}