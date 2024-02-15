using Ae.Dns.Client.Lookup;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class DnsZoneLookupTests
    {
        private sealed class DummyZoneNoRecords : IDnsZone
        {
            public IList<DnsResourceRecord> Records { get; set; } = new List<DnsResourceRecord>();
            public DnsLabels Origin { get; set; }
            public TimeSpan? DefaultTtl { get; set; }
            public Task<TResult> Update<TResult>(Func<TResult> modification) => throw new NotImplementedException();
        }

        [Fact]
        public void TestLookupNoResults()
        {
            var lookup = new DnsZoneLookupSource(new DummyZoneNoRecords());

            Assert.False(lookup.TryReverseLookup(IPAddress.Loopback, out var hostnames));
            Assert.Empty(hostnames);
            Assert.False(lookup.TryForwardLookup("wibble", out var addresses));
            Assert.Empty(addresses);
        }

        private sealed class DummyZoneWithRecords : IDnsZone
        {
            public IList<DnsResourceRecord> Records { get; set; } = new[]
            {
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.A, Resource = new DnsIpAddressResource { IPAddress = IPAddress.Loopback } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.A, Resource = new DnsIpAddressResource { IPAddress = IPAddress.Broadcast } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.TEXT, Resource = new DnsTextResource { Entries = "hello1" } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.TEXT, Resource = new DnsTextResource { Entries = "hello2" } },
            };

            public DnsLabels Origin { get; set; }
            public TimeSpan? DefaultTtl { get; set; }
            public Task<TResult> Update<TResult>(Func<TResult> modification) => throw new NotImplementedException();
        }

        [Fact]
        public void TestLookupWithResults()
        {
            var lookup = new DnsZoneLookupSource(new DummyZoneWithRecords());

            Assert.True(lookup.TryReverseLookup(IPAddress.Loopback, out var hostnames));
            Assert.Equal(new[] { "wibble" }, hostnames);
            Assert.False(lookup.TryForwardLookup("wibble", out var addresses));
            Assert.Empty(addresses);
        }
    }
}
