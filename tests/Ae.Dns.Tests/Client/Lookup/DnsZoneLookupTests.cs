#pragma warning disable CS0618 // Type or member is obsolete

using Ae.Dns.Client.Lookup;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class DnsZoneLookupTests
    {
        private sealed class DummyZoneNoRecords : IDnsZone
        {
            public IList<DnsResourceRecord> Records { get; set; } = new List<DnsResourceRecord>();
            public DnsLabels Origin { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public TimeSpan DefaultTtl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Task<bool> ChangeRecords(Action<ICollection<DnsResourceRecord>> changeDelegate, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }

            public void DeserializeZone(StreamReader reader)
            {
                throw new NotImplementedException();
            }

            public string FromFormattedHost(string host)
            {
                throw new NotImplementedException();
            }

            public void SerializeZone(StreamWriter writer)
            {
                throw new NotImplementedException();
            }

            public string ToFormattedHost(string host)
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
            public IList<DnsResourceRecord> Records { get; set; } = new[]
            {
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.A, Resource = new DnsIpAddressResource { IPAddress = IPAddress.Loopback } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.A, Resource = new DnsIpAddressResource { IPAddress = IPAddress.Broadcast } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.TEXT, Resource = new DnsTextResource { Entries = "hello1" } },
                new DnsResourceRecord { Host = "wibble", Class = DnsQueryClass.IN, Type = DnsQueryType.TEXT, Resource = new DnsTextResource { Entries = "hello2" } },
            };

            public DnsLabels Origin { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public TimeSpan DefaultTtl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Task<bool> ChangeRecords(Action<ICollection<DnsResourceRecord>> changeDelegate, CancellationToken token = default)
            {
                throw new NotImplementedException();
            }

            public void DeserializeZone(StreamReader reader)
            {
                throw new NotImplementedException();
            }

            public string FromFormattedHost(string host)
            {
                throw new NotImplementedException();
            }

            public void SerializeZone(StreamWriter writer)
            {
                throw new NotImplementedException();
            }

            public string ToFormattedHost(string host)
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
