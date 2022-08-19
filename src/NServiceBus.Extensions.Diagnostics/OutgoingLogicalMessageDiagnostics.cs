using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class OutgoingLogicalMessageDiagnostics : Behavior<IOutgoingLogicalMessageContext>
    {
        private readonly DiagnosticListener _diagnosticListener;
        private const string EventName = ActivityNames.OutgoingLogicalMessage + ".Sent";

        public OutgoingLogicalMessageDiagnostics(DiagnosticListener diagnosticListener)
            => _diagnosticListener = diagnosticListener;

        public OutgoingLogicalMessageDiagnostics()
            : this(new DiagnosticListener(ActivityNames.OutgoingLogicalMessage)) { }

        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            using var activity = StartActivity();

            using var currentContextActivity = new CurrentContextActivity(activity);

            try
            {
                context.Extensions.Set<ICurrentActivity>(currentContextActivity);

                await next().ConfigureAwait(false);
            }
            finally
            {
                context.Extensions.Remove<ICurrentActivity>();
            }

            if (_diagnosticListener.IsEnabled(EventName))
            {
                _diagnosticListener.Write(EventName, context);
            }
        }

        private static Activity? StartActivity()
        {
            Activity? activity = null;
            if (NServiceBusActivitySource.ActivitySource.HasListeners())
            {
                activity = NServiceBusActivitySource.ActivitySource.CreateActivity(ActivityNames.OutgoingLogicalMessage, ActivityKind.Client);
            }

            if (activity is null && Activity.Current is not null)
            {
                activity = new Activity(ActivityNames.OutgoingLogicalMessage);
            }

            activity?.Start();
            return activity;
        }
    }
}