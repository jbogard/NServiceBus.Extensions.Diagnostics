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
            context.Pipeline.Register(new IncomingPhysicalMessageDiagnostics(new DiagnosticListener(ActivityNames.IncomingPhysicalMessage)), "Parses incoming W3C trace information from incoming messages.");
            context.Pipeline.Register(new OutgoingPhysicalMessageDiagnostics(new DiagnosticListener(ActivityNames.OutgoingPhysicalMessage)), "Appends W3C trace information to outgoing messages.");
            context.Pipeline.Register(new IncomingLogicalMessageDiagnostics(new DiagnosticListener(ActivityNames.IncomingLogicalMessage)), "Raises diagnostic events for successfully processed messages.");
            context.Pipeline.Register(new OutgoingLogicalMessageDiagnostics(new DiagnosticListener(ActivityNames.OutgoingLogicalMessage)), "Raises diagnostic events for successfully sent messages.");
        }
    }
}