using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Testing;
using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    public class OutgoingMessagePipelineTests
    {
        [Fact]
        public async void Should_custom_enrich_outgoing_activity()
        {
            Activity started = null;

            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "NServiceBus.Extensions.Diagnostics",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => started = activity
            };
            ActivitySource.AddActivityListener(listener);

            var logicalContext = new TestableOutgoingLogicalMessageContext();
            var physicalContext = new TestableOutgoingPhysicalMessageContext();

            var enricher = new TestEnricher((activity, _) => activity.AddTag("Key1", "Value1"));

            var logicalBehavior = new OutgoingLogicalMessageDiagnostics();
            var customEnrichingBehavior = new DelegateBehavior<IOutgoingPhysicalMessageContext>(async (_, next) =>
            {
                var activity = Activity.Current;
                activity.ShouldNotBeNull("Cannot enrich a null Activity");
                activity.AddTag("Key2", "Value2");
                await next();
            });
            var physicalBehavior = new OutgoingPhysicalMessageDiagnostics(enricher);

            // Simulates NServiceBus outgoing message pipeline.
            // The order of the outgoing pipeline is IOutgoingLogicalMessageContext and then IOutgoingPhysicalMessageContext:
            // https://docs.particular.net/nservicebus/pipeline/steps-stages-connectors#stages-outgoing-pipeline-stages.
            // Implementations of IMutateOutgoingTransportMessages are executed in the pipeline
            // after OutgoingLogicalMessageDiagnostics and before OutgoingPhysicalMessageDiagnostics.
            await logicalBehavior.Invoke(logicalContext,
                () => customEnrichingBehavior.Invoke(physicalContext,
                    () => physicalBehavior.Invoke(physicalContext, () => Task.CompletedTask)));

            started.ShouldNotBeNull();
            started.Tags.ShouldContain(kv => kv.Key == "Key1");
            started.Tags.ShouldContain(kv => kv.Key == "Key2");
        }

        private class DelegateBehavior<TContext> : Behavior<TContext>
            where TContext : class, IBehaviorContext
        {
            private readonly Func<TContext, Func<Task>, Task> _invocation;

            public DelegateBehavior(Func<TContext, Func<Task>, Task> invocation)
            {
                _invocation = invocation;
            }

            public override Task Invoke(TContext context, Func<Task> next)
            {
                return _invocation(context, next);
            }
        }

        private class TestEnricher : IActivityEnricher
        {
            private readonly Action<Activity, IOutgoingPhysicalMessageContext> _outgoingEnrichment;

            public TestEnricher(Action<Activity, IOutgoingPhysicalMessageContext> outgoingEnrichment)
            {
                _outgoingEnrichment = outgoingEnrichment;
            }

            public void Enrich(Activity activity, IIncomingPhysicalMessageContext context)
            {
                throw new NotImplementedException();
            }

            public void Enrich(Activity activity, IOutgoingPhysicalMessageContext context)
            {
                _outgoingEnrichment(activity, context);
            }
        }
    }
}
