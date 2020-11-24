using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Testing;
using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    public class IncomingLogicalMessageDiagnosticsTests
    {
        [Fact]
        public async Task Should_not_fire_activity_start_stop_when_no_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableIncomingLogicalMessageContext();
            var processedFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
                {
                    // This should not fire
                    if (pair.Key == $"{ActivityNames.IncomingLogicalMessage}.Processed")
                    {
                        processedFired = true;
                    }
                }),
                (s, o, arg3) => false);

            var behavior = new IncomingLogicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            processedFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            using var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableIncomingLogicalMessageContext();
            var processedFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.IncomingLogicalMessage}.Processed")
                {
                    processedFired = true;
                }
            }));

            var behavior = new IncomingLogicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            processedFired.ShouldBeTrue();
        }
    }
}