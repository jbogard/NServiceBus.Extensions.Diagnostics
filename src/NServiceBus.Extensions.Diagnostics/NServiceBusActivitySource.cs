using System;
using System.Diagnostics;
using System.Reflection;

namespace NServiceBus.Extensions.Diagnostics
{
    internal static class NServiceBusActivitySource
    {
        internal static readonly AssemblyName AssemblyName = typeof(NServiceBusActivitySource).Assembly.GetName();
        internal static readonly string ActivitySourceName = AssemblyName.Name;
        internal static readonly Version Version = AssemblyName.Version;
        internal static readonly ActivitySource ActivitySource = new (ActivitySourceName, Version.ToString());
    }
}