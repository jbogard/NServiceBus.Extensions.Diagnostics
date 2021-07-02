using System;
using System.Diagnostics;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.Diagnostics
{
    public class InstrumentationOptions
    {
        public bool CaptureMessageBody { get; set; }

        public Action<Activity, IIncomingPhysicalMessageContext>? EnrichIncoming { get; set; }

        public Action<Activity, IOutgoingPhysicalMessageContext>? EnrichOutgoing { get; set; }
    }
}