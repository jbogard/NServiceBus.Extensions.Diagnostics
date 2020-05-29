using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class IncomingLogicalMessageDiagnostics : Behavior<IIncomingLogicalMessageContext>
    {
        private static readonly DiagnosticSource _diagnosticListener = new DiagnosticListener(ActivityNames.IncomingLogicalMessage);

        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            if (_diagnosticListener.IsEnabled("Processed"))
            {
                _diagnosticListener.Write("Processed", context);
            }
        }
    }
}