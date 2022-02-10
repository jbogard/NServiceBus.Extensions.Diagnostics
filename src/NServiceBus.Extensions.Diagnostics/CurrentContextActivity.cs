using System;
using System.Diagnostics;

namespace NServiceBus.Extensions.Diagnostics;

class CurrentContextActivity : ICurrentActivity, IDisposable
{
    public CurrentContextActivity(Activity? current) => Current = current;

    public Activity? Current { get; private set; }

    public void Dispose()
    {
        Current?.Dispose();
        Current = null;
    }
}