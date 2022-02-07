using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Testing;
using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    public class OutgoingLogicalMessageDiagnosticsTests
    {
        [Fact]
        public async Task Should_not_fire_activity_start_stop_when_no_listener_attached()
        {
            var context = new TestableOutgoingLogicalMessageContext();
            var stopFired = false;
            var startFired = false;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Nonsense",
                ActivityStarted = _ => startFired = true,
                ActivityStopped = _ => stopFired = true
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new OutgoingLogicalMessageDiagnostics();

            await behavior.Invoke(context, () => Task.CompletedTask);

            startFired.ShouldBeFalse();
            stopFired.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_fire_activity_start_stop_when_listener_attached()
        {
            var context = new TestableOutgoingLogicalMessageContext();
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

            var behavior = new OutgoingLogicalMessageDiagnostics();

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
                    activity.OperationName.ShouldBe(ActivityNames.OutgoingLogicalMessage);
                },
            };
            ActivitySource.AddActivityListener(listener);

            var context = new TestableOutgoingLogicalMessageContext();

            var behavior = new OutgoingLogicalMessageDiagnostics();

            await behavior.Invoke(context, () => Task.CompletedTask);

            startCalled.ShouldBeTrue();
        }
    }
}