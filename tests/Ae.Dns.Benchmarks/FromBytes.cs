using Ae.Dns.Protocol;
using Ae.Dns.Tests;
using BenchmarkDotNet.Attributes;

namespace Ae.Dns.Benchmarks
{
    public class FromBytes
    {
        [Benchmark]
        public DnsMessage Query1() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query1);

        [Benchmark]
        public DnsMessage Query2() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query2);

        [Benchmark]
        public DnsMessage Query3() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query3);

        [Benchmark]
        public DnsMessage Query4() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query4);

        [Benchmark]
        public DnsMessage Query5() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query5);

        [Benchmark]
        public DnsMessage Query6() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query6);

        [Benchmark]
        public DnsMessage Query7() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Query7);

        [Benchmark]
        public DnsMessage Answer1() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer1);

        [Benchmark]
        public DnsMessage Answer2() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer2);

        [Benchmark]
        public DnsMessage Answer3() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer3);

        [Benchmark]
        public DnsMessage Answer4() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer4);

        [Benchmark]
        public DnsMessage Answer5() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer5);

        [Benchmark]
        public DnsMessage Answer6() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer6);

        [Benchmark]
        public DnsMessage Answer7() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer7);

        [Benchmark]
        public DnsMessage Answer8() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer8);

        [Benchmark]
        public DnsMessage Answer9() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer9);

        [Benchmark]
        public DnsMessage Answer10() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer10);

        [Benchmark]
        public DnsMessage Answer11() => DnsByteExtensions.FromBytes<DnsMessage>(SampleDnsPackets.Answer11);
    }
}
