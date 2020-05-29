using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class OutgoingPhysicalMessageDiagnostics : Behavior<IOutgoingPhysicalMessageContext>
    {
        private static readonly DiagnosticSource _diagnosticListener = new DiagnosticListener(ActivityNames.OutgoingPhysicalMessage);

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

        private static Activity StartActivity(IOutgoingPhysicalMessageContext context)
        {
            var activity = new Activity(ActivityNames.OutgoingPhysicalMessage);

            _diagnosticListener.OnActivityImport(activity, context);

            if (_diagnosticListener.IsEnabled("Start", context))
            {
                _diagnosticListener.StartActivity(activity, context);
            }
            else
            {
                activity.Start();
            }

            foreach (var header in context.Headers.Where(kvp => kvp.Key.StartsWith("NServiceBus")))
            {
                activity.AddTag(header.Key.Replace("NServiceBus.", ""), header.Value);
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
        }

        private static void StopActivity(Activity activity, IOutgoingPhysicalMessageContext context)
        {
            _diagnosticListener.StopActivity(activity, context);
        }
    }
}