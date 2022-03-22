using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using NServiceBus.Features;
using NServiceBus.Transport;

namespace NServiceBus.Extensions.Diagnostics;

public class DiagnosticsMetricsFeature : Feature
{
    private static readonly AssemblyName AssemblyName = typeof(DiagnosticsMetricsFeature).Assembly.GetName();
    private static readonly string InstrumentationName = AssemblyName.Name;
    private static readonly string InstrumentationVersion = AssemblyName.Version.ToString();
    private static readonly Meter NServiceBusMeter = new(InstrumentationName, InstrumentationVersion);

    private static readonly Counter<long> SuccessTotalCounter =
        NServiceBusMeter.CreateCounter<long>("messaging.successes");
    private static readonly Counter<long> FetchedTotalCounter =
        NServiceBusMeter.CreateCounter<long>("messaging.fetches");
    private static readonly Counter<long> FailureTotalCounter =
        NServiceBusMeter.CreateCounter<long>("messaging.failures");
    private static readonly Histogram<double> CriticalTimeSecondsHistogram =
        NServiceBusMeter.CreateHistogram<double>("messaging.client_server.duration", "ms", "Lead time");
    private static readonly Histogram<double> ProcessingTimeSecondsHistogram =
        NServiceBusMeter.CreateHistogram<double>("messaging.server.duration", "ms", "Cycle or processing time");
    private static readonly Counter<long> RetriesTotalCounter =
        NServiceBusMeter.CreateCounter<long>("messaging.retries");

    private MetricsOptions? _metricsOptions;

    private static Dictionary<string, Counter<long>> SignalMapping = new()
    {
        { "# of msgs successfully processed / sec", SuccessTotalCounter },
        { "# of msgs pulled from the input queue /sec", FetchedTotalCounter },
        { "# of msgs failures / sec", FailureTotalCounter },
        { "Retries", RetriesTotalCounter },
    };
    private static Dictionary<string, Histogram<double>> DurationMapping = new()
    {
        { "Critical Time", CriticalTimeSecondsHistogram },
        { "Processing Time", ProcessingTimeSecondsHistogram },
    };

    public DiagnosticsMetricsFeature()
    {
        Defaults(settings =>
        {
            _metricsOptions = settings.EnableMetrics();
        });
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

        var logicalAddress = settings.LogicalAddress();
        var discriminator = logicalAddress.EndpointInstance.Discriminator ?? "none";

        var transportDefinition = settings.Get<TransportDefinition>();

        _metricsOptions?.RegisterObservers(
            register: probeContext =>
            {
                RegisterProbes(probeContext, new[]
                {
                    new KeyValuePair<string, object?>("messaging.system", transportDefinition.GetType().Name.Replace("Transport", null).ToLowerInvariant()),
                    new KeyValuePair<string, object?>("messaging.destination", settings.LocalAddress()),
                    new KeyValuePair<string, object?>("messaging.destination.endpoint", settings.EndpointName()),
                    new KeyValuePair<string, object?>("messaging.destination.discriminator", discriminator),
                    new KeyValuePair<string, object?>("net.host.name", Environment.MachineName),
                    new KeyValuePair<string, object?>("net.host.ip", Dns.GetHostName())
                });
            });
    }

    private void RegisterProbes(ProbeContext context, KeyValuePair<string, object?>[] tags)
    {
        foreach (var duration in context.Durations)
        {
            if (!DurationMapping.ContainsKey(duration.Name))
            {
                continue;
            }
            var counter = DurationMapping[duration.Name];

            duration.Register((ref DurationEvent @event) => counter.Record(@event.Duration.TotalSeconds, tags));
        }

        foreach (var signal in context.Signals)
        {
            if (!SignalMapping.ContainsKey(signal.Name))
            {
                continue;
            }
            var counter = SignalMapping[signal.Name];

            signal.Register((ref SignalEvent @event) => counter.Add(1, tags));
        }
    }

}