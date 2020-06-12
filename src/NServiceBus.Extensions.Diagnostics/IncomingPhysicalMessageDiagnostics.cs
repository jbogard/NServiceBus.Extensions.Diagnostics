using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class IncomingPhysicalMessageDiagnostics : Behavior<IIncomingPhysicalMessageContext>
    {
        private readonly DiagnosticListener _diagnosticListener;

        private const string StartActivityName = ActivityNames.IncomingPhysicalMessage + ".Start";
        private const string StopActivityName = ActivityNames.IncomingPhysicalMessage + ".Stop";

        public IncomingPhysicalMessageDiagnostics(DiagnosticListener diagnosticListener) 
            => _diagnosticListener = diagnosticListener;

        public IncomingPhysicalMessageDiagnostics() 
            : this(new DiagnosticListener(ActivityNames.IncomingPhysicalMessage)) { }

        public override async Task Invoke(
            IIncomingPhysicalMessageContext context,
            Func<Task> next)
        {
            var activity = StartActivity(context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private Activity StartActivity(IIncomingPhysicalMessageContext context)
        {
            var activity = new Activity(ActivityNames.IncomingPhysicalMessage);

            if (!context.MessageHeaders.TryGetValue(Headers.TraceParentHeaderName, out var requestId))
            {
                context.MessageHeaders.TryGetValue(Headers.RequestIdHeaderName, out requestId);
            }

            if (!string.IsNullOrEmpty(requestId))
            {
                activity.SetParentId(requestId);

                if (context.MessageHeaders.TryGetValue(Headers.TraceStateHeaderName, out var traceState))
                {
                    activity.TraceStateString = traceState;
                }

                if (context.MessageHeaders.TryGetValue(Headers.CorrelationContextHeaderName, out var correlationContext))
                {
                    var baggage = correlationContext.Split(',');
                    if (baggage.Length > 0)
                    {
                        foreach (var item in baggage)
                        {
                            if (NameValueHeaderValue.TryParse(item, out var baggageItem))
                            {
                                activity.AddBaggage(baggageItem.Name, HttpUtility.UrlDecode(baggageItem.Value));
                            }
                        }
                    }
                }
            }

            _diagnosticListener.OnActivityImport(activity, context);

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

        private void StopActivity(Activity activity, IIncomingPhysicalMessageContext context)
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