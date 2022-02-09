using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task Should_start_activity_and_parse_appropriate_headers()
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
                    {"baggage", "Key1=value1, Key2=value2"}
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

        [Fact]
        public async Task Should_start_activity_and_parse_legacy_header()
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

            var context = new TestableIncomingPhysicalMessageContext
            {
                MessageHeaders =
                {
                    {"traceparent", id},
                    {"Correlation-Context", "Key1=value1, Key2=value2"}
                }
            };

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            started.ShouldNotBeNull();
            started.ParentId.ShouldBe(id);
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key1" && kvp.Value == "value1");
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key2" && kvp.Value == "value2");
        }

        [Fact]
        public async Task Should_start_activity_and_prefer_new_header()
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

            var context = new TestableIncomingPhysicalMessageContext
            {
                MessageHeaders =
                {
                    {"traceparent", id},
                    {"Correlation-Context", "Key1=value1, Key2=value2"},
                    {"baggage", "Key3=value1, Key4=value2"}
                }
            };

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            started.ShouldNotBeNull();
            started.ParentId.ShouldBe(id);
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key3" && kvp.Value == "value1");
            started.Baggage.ShouldContain(kvp => kvp.Key == "Key4" && kvp.Value == "value2");
            started.Baggage.ShouldNotContain(kvp => kvp.Key == "Key1");
            started.Baggage.ShouldNotContain(kvp => kvp.Key == "Key2");
        }

        [Fact]
        public async Task Should_preserve_baggage_order()
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

            var context = new TestableIncomingPhysicalMessageContext
            {
                MessageHeaders =
                {
                    {"traceparent", id},
                    {"Correlation-Context", "Key1=value1, Key2=value2"},
                    {"baggage", "Key3=value1, Key4=value2, Key4=value3"}
                }
            };

            var behavior = new IncomingPhysicalMessageDiagnostics(new FakeActivityEnricher());

            await behavior.Invoke(context, () => Task.CompletedTask);

            started.ShouldNotBeNull();
            var startedBaggage = started.Baggage.ToList();
            startedBaggage[0].Key.ShouldBe("Key3");
            startedBaggage[0].Value.ShouldBe("value1");
            startedBaggage[1].Key.ShouldBe("Key4");
            startedBaggage[1].Value.ShouldBe("value2");
            startedBaggage[2].Key.ShouldBe("Key4");
            startedBaggage[2].Value.ShouldBe("value3");
            startedBaggage.Count.ShouldBe(3);
        }
    }
}
