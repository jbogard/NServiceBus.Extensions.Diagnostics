using System.Diagnostics;
using NServiceBus.Pipeline;
using Xunit;

[assembly:CollectionBehavior(DisableTestParallelization = true)]

namespace NServiceBus.Extensions.Diagnostics.Tests
{
    internal class FakeActivityEnricher : IActivityEnricher
    {
        public void Enrich(Activity activity, IIncomingPhysicalMessageContext context)
        {

        }

        public void Enrich(Activity activity, IOutgoingPhysicalMessageContext context)
        {

        }
    }
}