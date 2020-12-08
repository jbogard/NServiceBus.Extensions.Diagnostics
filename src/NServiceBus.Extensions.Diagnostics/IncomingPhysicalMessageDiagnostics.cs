using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class IncomingPhysicalMessageDiagnostics : Behavior<IIncomingPhysicalMessageContext>
    {
        private readonly IActivityEnricher _activityEnricher;
        private readonly DiagnosticListener _diagnosticListener;
        private const string EventName = ActivityNames.IncomingPhysicalMessage + ".Processed";

        public IncomingPhysicalMessageDiagnostics(IActivityEnricher activityEnricher)
        {
            _diagnosticListener = new DiagnosticListener(ActivityNames.IncomingPhysicalMessage);
            _activityEnricher = activityEnricher;
        }

        public override async Task Invoke(
            IIncomingPhysicalMessageContext context,
            Func<Task> next)
        {
            using (StartActivity(context))
            {
                await next().ConfigureAwait(false);

                if (_diagnosticListener.IsEnabled(EventName))
                {
                    _diagnosticListener.Write(EventName, context);
                }
            }
        }

        private Activity? StartActivity(IIncomingPhysicalMessageContext context)
        {
            if (!context.MessageHeaders.TryGetValue(Headers.TraceParentHeaderName, out var parentId))
            {
                context.MessageHeaders.TryGetValue(Headers.RequestIdHeaderName, out parentId);
            }

            string? traceStateString = default;
            List<KeyValuePair<string, string?>> baggageItems = new();

            if (!string.IsNullOrEmpty(parentId))
            {
                if (context.MessageHeaders.TryGetValue(Headers.TraceStateHeaderName, out var traceState))
                {
                    traceStateString = traceState;
                }

                if (context.MessageHeaders.TryGetValue(Headers.BaggageHeaderName, out var baggageValue)
                   || context.MessageHeaders.TryGetValue(Headers.CorrelationContextHeaderName, out baggageValue))
                {
                    var baggage = baggageValue.Split(',');
                    if (baggage.Length > 0)
                    {
                        foreach (var item in baggage)
                        {
                            if (NameValueHeaderValue.TryParse(item, out var baggageItem))
                            {
                                baggageItems.Add(new KeyValuePair<string, string?>(baggageItem.Name, HttpUtility.UrlDecode(baggageItem.Value)));
                            }
                        }
                    }
                }
            }

            var activity = parentId == null
                ? NServiceBusActivitySource.ActivitySource.StartActivity(ActivityNames.IncomingPhysicalMessage, ActivityKind.Consumer)
                : NServiceBusActivitySource.ActivitySource.StartActivity(ActivityNames.IncomingPhysicalMessage, ActivityKind.Consumer, parentId);

            if (activity == null)
            {
                return activity;
            }

            activity.TraceStateString = traceStateString;

            _activityEnricher.Enrich(activity, context);

            foreach (var baggageItem in baggageItems)
            {
                activity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }

            return activity;
        }
    }
}