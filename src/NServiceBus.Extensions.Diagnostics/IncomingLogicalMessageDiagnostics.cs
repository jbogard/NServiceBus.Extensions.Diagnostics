using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class IncomingLogicalMessageDiagnostics : Behavior<IIncomingLogicalMessageContext>
    {
        private readonly DiagnosticListener _diagnosticListener;
        private const string EventName = ActivityNames.IncomingLogicalMessage + ".Processed";

        public IncomingLogicalMessageDiagnostics(DiagnosticListener diagnosticListener)
            => _diagnosticListener = diagnosticListener;

        public IncomingLogicalMessageDiagnostics()
            : this(new DiagnosticListener(ActivityNames.IncomingLogicalMessage))
        {
        }

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            if (_diagnosticListener.IsEnabled(EventName))
            {
                _diagnosticListener.Write(EventName, context);
            }
        }
    }
}