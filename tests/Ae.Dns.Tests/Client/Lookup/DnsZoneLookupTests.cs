using Ae.Dns.Client.Lookup;
using Ae.Dns.Client.Zone;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class DnsZoneLookupTests
    {
        private sealed class DummyZoneNoRecords : IDnsZone
        {
            public IEnumerable<DnsResourceRecord> Records => Enumerable.Empty<DnsResourceRecord>();

            public string Name => throw new NotImplementedException();

            public Task<bool> ChangeRecords(Action<ICollection<DnsResourceRecord>> changeDelegate, IEnumerable<DnsResourceRecord> recordsToAdd, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void TestLookupNoResults()
        {
            var lookup = new DnsZoneLookup(new DummyZoneNoRecords());

            Assert.False(lookup.TryReverseLookup(IPAddress.Loopback, out var hostnames));
            Assert.Empty(hostnames);
            Assert.False(lookup.TryForwardLookup("wibble", out var addresses));
            Assert.Empty(addresses);
        }

        private sealed class DummyZoneWithRecords : IDnsZone
        {
            public IEnumerable<DnsResourceRecord> Records => new[]
            {
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.A, Resource = new DnsIpAddressResource { IPAddress = IPAddress.Loopback } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.A, Resource = new DnsIpAddressResource { IPAddress = IPAddress.Broadcast } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.TEXT, Resource = new DnsTextResource { Entries = "hello1" } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.TEXT, Resource = new DnsTextResource { Entries = "hello2" } },
            };

            public string Name => throw new NotImplementedException();

            public Task<bool> ChangeRecords(Action<ICollection<DnsResourceRecord>> changeDelegate, IEnumerable<DnsResourceRecord> recordsToAdd, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void TestLookupWithResults()
        {
            var lookup = new DnsZoneLookup(new DummyZoneWithRecords());

            Assert.True(lookup.TryReverseLookup(IPAddress.Loopback, out var hostnames));
            Assert.Equal(new[] { "wibble" }, hostnames);
            Assert.True(lookup.TryForwardLookup("wibble", out var addresses));
            Assert.Equal(new[] { IPAddress.Loopback, IPAddress.Broadcast }, addresses);
        }
    }
}
