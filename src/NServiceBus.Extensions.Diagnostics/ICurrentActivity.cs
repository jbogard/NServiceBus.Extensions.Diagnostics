using System.Diagnostics;

namespace NServiceBus.Extensions.Diagnostics;

public interface ICurrentActivity
{
    public Activity? Current { get; }
}