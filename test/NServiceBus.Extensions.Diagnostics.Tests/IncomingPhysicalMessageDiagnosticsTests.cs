using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
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
            var context = new TestableIncomingPhysicalMessageContext();
            var stopFired = false;
            var startFired = false;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Nonsense",
                ActivityStarted = _ => startFired = true,
                ActivityStopped = _ => stopFired = true
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeFalse();
            stopFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var context = new TestableIncomingPhysicalMessageContext();
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

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

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
                    activity.OperationName.ShouldBe(ActivityNames.IncomingPhysicalMessage);
                },
            };
            ActivitySource.AddActivityListener(listener);

            var context = new TestableIncomingPhysicalMessageContext();

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_add_headers_to_tags()
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
                    activity.Tags.ShouldNotContain(kvp => kvp.Key == "foo");
                },
            };
            ActivitySource.AddActivityListener(listener);

            var context = new TestableIncomingPhysicalMessageContext
            {
                MessageHeaders =
                {
                    {"foo", "bar"},
                }
            };

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_start_activity_and_set_appropriate_headers()
        {
            // Generate an id we can use for the request id header (in the correct format)
            using var dummy = new Activity("IncomingRequest");
            dummy.Start();
            var id = dummy.Id;
            dummy.Stop();

            Activity started = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "NServiceBus.Extensions.Diagnostics",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => started = activity,
            };
            ActivitySource.AddActivityListener(listener);

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

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            started.ShouldNotBeNull();
            started.ParentId.ShouldBe(id);
            started.TraceStateString.ShouldBe(tracestate);
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key1" && kvp.Value == "value1");
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key2" && kvp.Value == "value2");
        }
    }
}
