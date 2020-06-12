using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
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
            var stopFired = false;
            var startFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
                {
                    // This should not fire
                    if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Start")
                    {
                        startFired = true;
                    }

                    // This should not fire
                    if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Stop")
                    {
                        stopFired = true;
                    }
                }),
                (s, o, arg3) => false);

            var behavior = new OutgoingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeFalse();
            stopFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableOutgoingPhysicalMessageContext();
            var stopFired = false;
            var startFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
                {
                    if (pair.Key == $"{ActivityNames.OutgoingPhysicalMessage}.Start")
                    {
                        startFired = true;
                    }

                    if (pair.Key == $"{ActivityNames.OutgoingPhysicalMessage}.Stop")
                    {
                        stopFired = true;
                    }
                }));

            var behavior = new OutgoingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeTrue();
            stopFired.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_start_and_log_activity()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var startCalled = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.OutgoingPhysicalMessage}.Start")
                {
                    startCalled = true;
                    pair.Value.ShouldNotBeNull();
                    Activity.Current.ShouldNotBeNull();
                    Activity.Current.OperationName.ShouldBe(ActivityNames.OutgoingPhysicalMessage);
                    pair.Value.ShouldBeAssignableTo<IOutgoingPhysicalMessageContext>();
                }
            }));

            var context = new TestableOutgoingPhysicalMessageContext();

            var behavior = new OutgoingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_start_activity_and_set_appropriate_headers()
        {
            // Generate an id we can use for the request id header (in the correct format)

            var diagnosticListener = new DiagnosticListener("DummySource");
            Activity started = null;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.OutgoingPhysicalMessage}.Start")
                {
                    started = Activity.Current;
                }
            }));

            var context = new TestableOutgoingPhysicalMessageContext();

            var behavior = new OutgoingPhysicalMessageDiagnostics(diagnosticListener);

            var activity = new Activity("Outer")
            {
                TraceStateString = "TraceStateValue",
            };
            activity.AddBaggage("Key1", "Value1");
            activity.AddBaggage("Key2", "Value2");
            activity.Start();

            await behavior.Invoke(context, () => Task.CompletedTask);

            activity.Stop();

            started.ShouldNotBeNull();
            started.ParentId.ShouldBe(activity.Id);

            context.Headers.ShouldContain(kvp => kvp.Key == "traceparent" && kvp.Value == started.Id);
            context.Headers.ShouldContain(kvp => kvp.Key == "tracestate" && kvp.Value == activity.TraceStateString);
            context.Headers.ShouldContain(kvp => kvp.Key == "Correlation-Context" && kvp.Value == "Key2=Value2,Key1=Value1");
        }
    }
}
