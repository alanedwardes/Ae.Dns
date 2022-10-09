using Ae.Dns.Protocol;
using Ae.Dns.Tests;
using BenchmarkDotNet.Running;
using System.Linq;

namespace Ae.Dns.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

            //for (var i = 0; i < 100_000; i++)
            //{
            //    foreach (var message in SampleDnsPackets.AllPackets.ToArray())
            //    {
            //        DnsByteExtensions.FromBytes<DnsMessage>(message);
            //    }
            //}
        }
    }
}
