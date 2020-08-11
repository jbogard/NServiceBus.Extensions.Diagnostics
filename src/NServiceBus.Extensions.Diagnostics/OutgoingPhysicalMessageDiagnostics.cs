using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class OutgoingPhysicalMessageDiagnostics : Behavior<IOutgoingPhysicalMessageContext>
    {
        private readonly DiagnosticListener _diagnosticListener;

        private const string StartActivityName = ActivityNames.OutgoingPhysicalMessage + ".Start";
        private const string StopActivityName = ActivityNames.OutgoingPhysicalMessage + ".Stop";

        public OutgoingPhysicalMessageDiagnostics(DiagnosticListener diagnosticListener)
            => _diagnosticListener = diagnosticListener;

        public OutgoingPhysicalMessageDiagnostics()
            : this(new DiagnosticListener(ActivityNames.OutgoingPhysicalMessage)) { }

        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            var activity = StartActivity(context);

            InjectHeaders(activity, context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private Activity StartActivity(IOutgoingPhysicalMessageContext context)
        {
            var activity = new Activity(ActivityNames.OutgoingPhysicalMessage);

            _diagnosticListener.OnActivityExport(activity, context);

            if (_diagnosticListener.IsEnabled(StartActivityName, context))
            {
                _diagnosticListener.StartActivity(activity, context);
            }
            else
            {
                activity.Start();
            }

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

            if (!context.Headers.ContainsKey(Headers.CorrelationContextHeaderName))
            {
                var baggage = string.Join(",", activity.Baggage.Select(item => $"{item.Key}={item.Value}"));
                if (!string.IsNullOrEmpty(baggage))
                {
                    context.Headers[Headers.CorrelationContextHeaderName] = baggage;
                }
            }
        }

        private void StopActivity(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            if (_diagnosticListener.IsEnabled(StopActivityName, context))
            {
                _diagnosticListener.StopActivity(activity, context);
            }
            else
            {
                activity.Stop();
            }
        }
    }
}