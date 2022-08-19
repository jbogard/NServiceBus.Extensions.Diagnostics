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
            : this(new DiagnosticListener(ActivityNames.OutgoingPhysicalMessage), activityEnricher)
        {
        }

        public OutgoingPhysicalMessageDiagnostics(DiagnosticListener diagnosticListener,
            IActivityEnricher activityEnricher)
        {
            _diagnosticListener = diagnosticListener;
            _activityEnricher = activityEnricher;
        }

        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            if (context.Extensions.TryGet<ICurrentActivity>(out var currentActivity) && currentActivity?.Current != null)
            {
                _activityEnricher.Enrich(currentActivity.Current, context);
                InjectHeaders(currentActivity.Current, context);
            }

            await next().ConfigureAwait(false);

            if (_diagnosticListener.IsEnabled(EventName))
            {
                _diagnosticListener.Write(EventName, context);
            }
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