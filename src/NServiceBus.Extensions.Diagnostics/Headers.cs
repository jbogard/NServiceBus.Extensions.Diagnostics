namespace NServiceBus.Extensions.Diagnostics
{
    internal static class Headers
    {
        public const string TraceParentHeaderName = "traceparent";
        public const string TraceStateHeaderName = "tracestate";
        public const string CorrelationContextHeaderName = "correlation-context";
        public const string RequestIdHeaderName = "Request-Id";
    }
}