using Ae.Dns.Protocol;
using Ae.Dns.Tests;
using BenchmarkDotNet.Attributes;
using System.Linq;

namespace Ae.Dns.Benchmarks
{
    public class ParseDnsQuery
    {
        [Benchmark]
        public void FromDnsPackets()
        {
            foreach (var packet in SampleDnsPackets.AllPackets.ToArray())
            {
                DnsByteExtensions.FromBytes<DnsMessage>(packet);
            }
        }
    }
}
