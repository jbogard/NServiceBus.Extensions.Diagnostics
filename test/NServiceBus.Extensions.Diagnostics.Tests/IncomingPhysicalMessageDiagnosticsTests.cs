using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Testing;
using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    public class IncomingPhysicalMessageDiagnosticsTests
    {
        static IncomingPhysicalMessageDiagnosticsTests()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
        }

        [Fact]
        public async Task Should_not_fire_activity_start_stop_when_no_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableIncomingPhysicalMessageContext();
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

            var behavior = new IncomingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeFalse();
            stopFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var context = new TestableIncomingPhysicalMessageContext();
            var stopFired = false;
            var startFired = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
                {
                    if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Start")
                    {
                        startFired = true;
                    }

                    if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Stop")
                    {
                        stopFired = true;
                    }
                }));

            var behavior = new IncomingPhysicalMessageDiagnostics(diagnosticListener);

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
                if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Start")
                {
                    startCalled = true;
                    pair.Value.ShouldNotBeNull();
                    Activity.Current.ShouldNotBeNull();
                    Activity.Current.OperationName.ShouldBe(ActivityNames.IncomingPhysicalMessage);
                    pair.Value.ShouldBeAssignableTo<IIncomingPhysicalMessageContext>();
                }
            }));

            var context = new TestableIncomingPhysicalMessageContext();

            var behavior = new IncomingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_add_headers_to_tags()
        {
            var diagnosticListener = new DiagnosticListener("DummySource");
            var startCalled = false;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Start")
                {
                    startCalled = true;

                    var started = Activity.Current;
                    started.ShouldNotBeNull();
                    started.Tags.ShouldNotContain(kvp => kvp.Key == "foo");
                }
            }));

            var context = new TestableIncomingPhysicalMessageContext
            {
                MessageHeaders =
                {
                    {"foo", "bar"},
                }
            };

            var behavior = new IncomingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_start_activity_and_set_appropriate_headers()
        {
            // Generate an id we can use for the request id header (in the correct format)
            var activity = new Activity("IncomingRequest");
            activity.Start();
            var id = activity.Id;
            activity.Stop();

            var diagnosticListener = new DiagnosticListener("DummySource");
            Activity started = null;

            diagnosticListener.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == $"{ActivityNames.IncomingPhysicalMessage}.Start")
                {
                    started = Activity.Current;
                }
            }));

            var tracestate = "TraceState";

            var context = new TestableIncomingPhysicalMessageContext
            {
                MessageHeaders =
                {
                    {"traceparent", id},
                    {"tracestate", tracestate},
                    {"Correlation-Context", "Key1=value1, Key2=value2"}
                }
            };

            var behavior = new IncomingPhysicalMessageDiagnostics(diagnosticListener);

            await behavior.Invoke(context, () => Task.CompletedTask);

            started.ShouldNotBeNull();
            started.ParentId.ShouldBe(id);
            started.TraceStateString.ShouldBe(tracestate);
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key1" && kvp.Value == "value1");
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key2" && kvp.Value == "value2");
        }
    }
}
