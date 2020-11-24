using System.Diagnostics;
using NServiceBus.Features;

namespace NServiceBus.Extensions.Diagnostics
{
    public class DiagnosticsFeature : Feature
    {
        public DiagnosticsFeature()
        {
            Defaults(settings => settings.SetDefault<InstrumentationOptions>(new InstrumentationOptions
            {
                CaptureMessageBody = false
            }));
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var activityEnricher = new SettingsActivityEnricher(context.Settings);

            context.Pipeline.Register(new IncomingPhysicalMessageDiagnostics(activityEnricher), "Parses incoming W3C trace information from incoming messages.");
            context.Pipeline.Register(new OutgoingPhysicalMessageDiagnostics(activityEnricher), "Appends W3C trace information to outgoing messages.");
            context.Pipeline.Register(new IncomingLogicalMessageDiagnostics(), "Raises diagnostic events for successfully processed messages.");
            context.Pipeline.Register(new OutgoingLogicalMessageDiagnostics(), "Raises diagnostic events for successfully sent messages.");
            context.Pipeline.Register(new InvokedHandlerDiagnostics(), "Raises diagnostic events when a handler/saga was invoked.");
        }
    }
}