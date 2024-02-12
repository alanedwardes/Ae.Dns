using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using Ae.Dns.Protocol.Zone;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Ae.Dns.Tests.Client
{
    public sealed class TestDnsZone : IDnsZone
    {
        private List<DnsResourceRecord> _records { get; set; } = new List<DnsResourceRecord>();
        public IReadOnlyList<DnsResourceRecord> Records => _records;
        public DnsLabels Origin { get => "example.com"; set => throw new NotImplementedException(); }
        public TimeSpan DefaultTtl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Func<IDnsZone, Task> ZoneUpdated { set => throw new NotImplementedException(); }
        public void DeserializeZone(string zone) => throw new NotImplementedException();
        public string FromFormattedHost(string host) => throw new NotImplementedException();
        public string SerializeZone() => throw new NotImplementedException();
        public string ToFormattedHost(string host) => throw new NotImplementedException();
        public Task Update(Action<IList<DnsResourceRecord>> modification)
        {
            modification(_records);
            return Task.CompletedTask;
        }
    }

    public sealed class DnsUpdateClientTests
    {
        [Fact]
        public async Task TestWrongZone()
        {
            var zone = new TestDnsZone();

            var updateClient = new DnsZoneUpdateClient(NullLogger<DnsZoneUpdateClient>.Instance, zone);

            var result = await updateClient.Query(new DnsMessage
            {
                Header = new DnsHeader { OperationCode = DnsOperationCode.UPDATE, QueryType = DnsQueryType.SOA, Host = "example.com" },
                Nameservers = new[]
                {
                    new DnsResourceRecord
                    {
                        Type = DnsQueryType.A,
                        Class = DnsQueryClass.IN,
                        Host = "test.google.com",
                        Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.168.0.1") }
                    }
                }
            });
            Assert.Equal(DnsResponseCode.Refused, result.Header.ResponseCode);

            Assert.Empty(zone.Records);
        }

        [Fact]
        public async Task TestWhitespaceHostname()
        {
            var zone = new TestDnsZone();

            var updateClient = new DnsZoneUpdateClient(NullLogger<DnsZoneUpdateClient>.Instance, zone);

            var result = await updateClient.Query(new DnsMessage
            {
                Header = new DnsHeader { OperationCode = DnsOperationCode.UPDATE, QueryType = DnsQueryType.SOA, Host = "example.com" },
                Nameservers = new[]
                {
                    new DnsResourceRecord
                    {
                        Type = DnsQueryType.A,
                        Class = DnsQueryClass.IN,
                        Host = "test me.example.com",
                        Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.168.0.1") }
                    }
                }
            });
            Assert.Equal(DnsResponseCode.Refused, result.Header.ResponseCode);

            Assert.Empty(zone.Records);
        }

        [Fact]
        public async Task TestAddRecords()
        {
            var zone = new TestDnsZone();

            var updateClient = new DnsZoneUpdateClient(NullLogger<DnsZoneUpdateClient>.Instance, zone);

            var result1 = await updateClient.Query(new DnsMessage
            {
                Header = new DnsHeader { OperationCode = DnsOperationCode.UPDATE, QueryType = DnsQueryType.SOA, Host = "example.com" },
                Nameservers = new[]
                {
                    new DnsResourceRecord
                    {
                        Type = DnsQueryType.A,
                        Class = DnsQueryClass.IN,
                        Host = "test.example.com",
                        Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.168.0.1") }
                    }
                }
            });
            Assert.Equal(DnsResponseCode.NoError, result1.Header.ResponseCode);

            var result2 = await updateClient.Query(new DnsMessage
            {
                Header = new DnsHeader { OperationCode = DnsOperationCode.UPDATE, QueryType = DnsQueryType.SOA, Host = "example.com" },
                Nameservers = new[]
                {
                    new DnsResourceRecord
                    {
                        Type = DnsQueryType.A,
                        Class = DnsQueryClass.IN,
                        Host = "test2.example.com",
                        Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.168.0.2") }
                    }
                }
            });
            Assert.Equal(DnsResponseCode.NoError, result2.Header.ResponseCode);

            var result3 = await updateClient.Query(new DnsMessage
            {
                Header = new DnsHeader { OperationCode = DnsOperationCode.UPDATE, QueryType = DnsQueryType.SOA, Host = "example.com" },
                Nameservers = new[]
                {
                    new DnsResourceRecord
                    {
                        Type = DnsQueryType.A,
                        Class = DnsQueryClass.IN,
                        Host = "test2.example.com",
                        Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.168.0.3") }
                    }
                }
            });
            Assert.Equal(DnsResponseCode.NoError, result3.Header.ResponseCode);

            var result4 = await updateClient.Query(new DnsMessage
            {
                Header = new DnsHeader { OperationCode = DnsOperationCode.UPDATE, QueryType = DnsQueryType.SOA, Host = "example.com" },
                Nameservers = new[]
                {
                    new DnsResourceRecord
                    {
                        Type = DnsQueryType.A,
                        Class = DnsQueryClass.IN,
                        Host = "test4.example.com",
                        Resource = new DnsIpAddressResource { IPAddress = IPAddress.Parse("192.168.0.1") }
                    }
                }
            });
            Assert.Equal(DnsResponseCode.NoError, result4.Header.ResponseCode);

            Assert.Equal(2, zone.Records.Count);
            Assert.Equal("test2.example.com", zone.Records[0].Host);
            Assert.Equal("192.168.0.3", zone.Records[0].Resource.ToString());
            Assert.Equal("test4.example.com", zone.Records[1].Host);
            Assert.Equal("192.168.0.1", zone.Records[1].Resource.ToString());
        }
    }
}
