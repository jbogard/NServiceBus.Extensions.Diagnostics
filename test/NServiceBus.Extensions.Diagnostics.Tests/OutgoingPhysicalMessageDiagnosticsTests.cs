using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Testing;
using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    public class OutgoingPhysicalMessageDiagnosticsTests
    {
        static OutgoingPhysicalMessageDiagnosticsTests()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
        }

        [Fact]
        public async Task Should_not_fire_activity_start_stop_when_no_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableOutgoingPhysicalMessageContext();
            var processedFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
                {
                    // This should not fire
                    if (pair.Key == $"{ActivityNames.OutgoingPhysicalMessage}.Sent")
                    {
                        processedFired = true;
                    }
                }),
                (_, _, _) => false);

            var behavior = new OutgoingPhysicalMessageDiagnostics(diagnosticListener, new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            processedFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableOutgoingPhysicalMessageContext();
            var processedFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.OutgoingPhysicalMessage}.Sent")
                {
                    processedFired = true;
                }
            }));

            var behavior = new OutgoingPhysicalMessageDiagnostics(diagnosticListener, new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            processedFired.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_set_appropriate_headers()
        {
            var context = new TestableOutgoingPhysicalMessageContext();

            var behavior = new OutgoingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            var outerActivity = new Activity("Outer")
            {
                TraceStateString = "TraceStateValue",
            };
            outerActivity.AddBaggage("Key1", "Value1");
            outerActivity.AddBaggage("Key2", "Value2");
            outerActivity.Start();

            await behavior.Invoke(context, () => Task.CompletedTask);

            outerActivity.Stop();

            context.Headers.ShouldContain(kvp => kvp.Key == "traceparent" && kvp.Value == outerActivity.Id);
            context.Headers.ShouldContain(kvp => kvp.Key == "tracestate" && kvp.Value == outerActivity.TraceStateString);
            context.Headers.ShouldContain(kvp => kvp.Key == "Correlation-Context" && kvp.Value == "Key2=Value2,Key1=Value1");
            context.Headers.ShouldContain(kvp => kvp.Key == "baggage" && kvp.Value == "Key2=Value2,Key1=Value1");
        }
    }
}
