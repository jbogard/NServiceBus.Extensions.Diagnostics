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
            using (StartActivity())
            {
                await next().ConfigureAwait(false);

                if (_diagnosticListener.IsEnabled(EventName))
                {
                    _diagnosticListener.Write(EventName, context);
                }
            }
        }

        private static Activity? StartActivity()
        {
            var activity = NServiceBusActivitySource.ActivitySource.StartActivity(ActivityNames.OutgoingLogicalMessage, ActivityKind.Producer);
            return activity;
        }
    }
}