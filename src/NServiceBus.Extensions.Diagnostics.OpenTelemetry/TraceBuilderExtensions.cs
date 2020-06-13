using System;
using OpenTelemetry.Trace.Configuration;

namespace NServiceBus.Extensions.Diagnostics.OpenTelemetry
{
    public static class TraceBuilderExtensions
    {
        public static TracerBuilder AddNServiceBusAdapter(this TracerBuilder builder)
            => builder.AddNServiceBusAdapter(null);

        public static TracerBuilder AddNServiceBusAdapter(this TracerBuilder builder, Action<NServiceBusInstrumentationOptions> configureInstrumentationOptions)
        {
            var options = new NServiceBusInstrumentationOptions();

            configureInstrumentationOptions ??= opt => { };
            
            configureInstrumentationOptions(options);
            
            return builder
                .AddAdapter(t => new NServiceBusReceiveAdapter(t, options))
                .AddAdapter(t => new NServiceBusSendAdapter(t, options));
        }
    }
}