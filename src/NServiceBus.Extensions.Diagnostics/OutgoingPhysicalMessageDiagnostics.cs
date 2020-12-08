using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class OutgoingPhysicalMessageDiagnostics : Behavior<IOutgoingPhysicalMessageContext>
    {
        private readonly IActivityEnricher _activityEnricher;
        private readonly DiagnosticListener _diagnosticListener;
        private const string EventName = ActivityNames.OutgoingPhysicalMessage + ".Sent";

        public OutgoingPhysicalMessageDiagnostics(IActivityEnricher activityEnricher)
        {
            _diagnosticListener = new DiagnosticListener(ActivityNames.OutgoingPhysicalMessage);
            _activityEnricher = activityEnricher;
        }

        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            using (var activity = StartActivity(context))
            {
                if (activity != null)
                {
                    InjectHeaders(activity, context);
                }

                await next().ConfigureAwait(false);

                if (_diagnosticListener.IsEnabled(EventName))
                {
                    _diagnosticListener.Write(EventName, context);
                }
            }
        }

        private Activity? StartActivity(IOutgoingPhysicalMessageContext context)
        {
            var activity = NServiceBusActivitySource.ActivitySource.StartActivity(ActivityNames.OutgoingPhysicalMessage, ActivityKind.Producer);

            if (activity == null)
            {
                return activity;
            }

            _activityEnricher.Enrich(activity, context);

            return activity;
        }

        private static void InjectHeaders(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                if (!context.Headers.ContainsKey(Headers.TraceParentHeaderName))
                {
                    context.Headers[Headers.TraceParentHeaderName] = activity.Id;
                    if (activity.TraceStateString != null)
                    {
                        context.Headers[Headers.TraceStateHeaderName] = activity.TraceStateString;
                    }
                }
            }
            else
            {
                if (!context.Headers.ContainsKey(Headers.RequestIdHeaderName))
                {
                    context.Headers[Headers.RequestIdHeaderName] = activity.Id;
                }
            }

            if (!context.Headers.ContainsKey(Headers.CorrelationContextHeaderName) 
                && !context.Headers.ContainsKey(Headers.BaggageHeaderName))
            {
                var baggage = string.Join(",", activity.Baggage.Select(item => $"{item.Key}={item.Value}"));
                if (!string.IsNullOrEmpty(baggage))
                {
                    context.Headers[Headers.CorrelationContextHeaderName] = baggage;
                    context.Headers[Headers.BaggageHeaderName] = baggage;
                }
            }
        }
    }
}