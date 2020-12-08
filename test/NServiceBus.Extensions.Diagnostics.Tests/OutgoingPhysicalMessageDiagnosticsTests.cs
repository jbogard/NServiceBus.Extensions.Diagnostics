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
            var context = new TestableOutgoingPhysicalMessageContext();
            var stopFired = false;
            var startFired = false;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Nonsense",
                ActivityStarted = _ => startFired = true,
                ActivityStopped = _ => stopFired = true
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new OutgoingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeFalse();
            stopFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var context = new TestableOutgoingPhysicalMessageContext();
            var stopFired = false;
            var startFired = false;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "NServiceBus.Extensions.Diagnostics",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = _ => startFired = true,
                ActivityStopped = _ => stopFired = true
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new OutgoingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeTrue();
            stopFired.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_start_and_log_activity()
        {
            var startCalled = false;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "NServiceBus.Extensions.Diagnostics",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity =>
                {
                    startCalled = true;
                    activity.ShouldNotBeNull();
                    activity.OperationName.ShouldBe(ActivityNames.OutgoingPhysicalMessage);
                },
            };
            ActivitySource.AddActivityListener(listener);

            var context = new TestableOutgoingPhysicalMessageContext();

            var behavior = new OutgoingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_start_activity_and_set_appropriate_headers()
        {
            // Generate an id we can use for the request id header (in the correct format)

            Activity started = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "NServiceBus.Extensions.Diagnostics",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => started = activity,
            };
            ActivitySource.AddActivityListener(listener);

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

            started.ShouldNotBeNull();
            started.ParentId.ShouldBe(outerActivity.Id);

            context.Headers.ShouldContain(kvp => kvp.Key == "traceparent" && kvp.Value == started.Id);
            context.Headers.ShouldContain(kvp => kvp.Key == "tracestate" && kvp.Value == outerActivity.TraceStateString);
            context.Headers.ShouldContain(kvp => kvp.Key == "Correlation-Context" && kvp.Value == "Key2=Value2,Key1=Value1");
            context.Headers.ShouldContain(kvp => kvp.Key == "baggage" && kvp.Value == "Key2=Value2,Key1=Value1");
        }
    }
}
