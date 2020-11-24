using System.Diagnostics;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public interface IActivityEnricher
    {
        void Enrich(Activity activity, IIncomingPhysicalMessageContext context);
        void Enrich(Activity activity, IOutgoingPhysicalMessageContext context);
    }
}