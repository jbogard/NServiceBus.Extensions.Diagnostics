using System.Diagnostics;
using NServiceBus.Features;

namespace NServiceBus.Extensions.Diagnostics
{
    public class DiagnosticsFeature : Feature
    {
        public DiagnosticsFeature()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(new IncomingPhysicalMessageDiagnostics(), "Parses incoming W3C trace information from incoming messages.");
            context.Pipeline.Register(new OutgoingPhysicalMessageDiagnostics(), "Appends W3C trace information to outgoing messages.");
            context.Pipeline.Register(new IncomingLogicalMessageDiagnostics(), "Raises diagnostic events for successfully processed messages.");
            context.Pipeline.Register(new OutgoingLogicalMessageDiagnostics(), "Raises diagnostic events for successfully sent messages.");
        }
    }
}