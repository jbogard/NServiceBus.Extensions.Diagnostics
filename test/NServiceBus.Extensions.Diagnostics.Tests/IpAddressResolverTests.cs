using Shouldly;
using Xunit;

namespace NServiceBus.Extensions.Diagnostics.Tests;

public class IpAddressResolverTests
{
    [Fact]
    public void Should_resolve_IP_address()
    {
        var ipAddress = IpAddressResolver.Value;

        ipAddress.ShouldNotBeNull();
;    }
}