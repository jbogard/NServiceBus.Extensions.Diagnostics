using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class InvokedHandlerDiagnostics : Behavior<IInvokeHandlerContext>
    {
        private readonly DiagnosticListener _diagnosticListener;
        private const string EventName = ActivityNames.InvokedHandler + ".Processed";

        public InvokedHandlerDiagnostics(DiagnosticListener diagnosticListener) => _diagnosticListener = diagnosticListener;

        public InvokedHandlerDiagnostics() : this(new DiagnosticListener(ActivityNames.InvokedHandler))
        {
        }

        public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            if (_diagnosticListener.IsEnabled(EventName))
            {
                _diagnosticListener.Write(EventName, context);
            }
        }
    }
}