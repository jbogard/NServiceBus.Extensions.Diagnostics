using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class OutgoingLogicalMessageDiagnostics : Behavior<IOutgoingLogicalMessageContext>
    {
        private static readonly DiagnosticSource _diagnosticListener = new DiagnosticListener(ActivityNames.OutgoingLogicalMessage);

        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            if (_diagnosticListener.IsEnabled("Sent"))
            {
                _diagnosticListener.Write("Sent", context);
            }
        }
    }
}