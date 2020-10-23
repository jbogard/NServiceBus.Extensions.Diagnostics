using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Testing;
using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    public class InvokedHandlerDiagnosticsTests
    {
        [Fact]
        public async Task Should_not_fire_activity_start_stop_when_no_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableInvokeHandlerContext();
            var processedFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
                {
                    // This should not fire
                    if (pair.Key == $"{ActivityNames.InvokedHandler}.Processed")
                    {
                        processedFired = true;
                    }
                }),
                (s, o, arg3) => false);

            var behavior = new InvokedHandlerDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            processedFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableInvokeHandlerContext();
            var processedFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.InvokedHandler}.Processed")
                {
                    processedFired = true;
                }
            }));

            var behavior = new InvokedHandlerDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            processedFired.ShouldBeTrue();
        }
        
    }
}