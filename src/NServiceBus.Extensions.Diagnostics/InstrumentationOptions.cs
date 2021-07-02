using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NServiceBus.Extensions.Diagnostics
{
    public class InstrumentationOptions
    {
        public bool CaptureMessageBody { get; set; }

        public Action<Activity, IReadOnlyDictionary<string, string>>? Enrich { get; set; }
    }
}