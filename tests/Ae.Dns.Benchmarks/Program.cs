using Ae.Dns.Protocol;
using Ae.Dns.Tests;
using System;
using System.Linq;

namespace Ae.Dns.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

            Span<byte> buffer = new byte[65527];

            for (var i = 0; i < 100_000; i++)
            {
                foreach (var message in SampleDnsPackets.AllPackets.ToArray())
                {
                    var reader = new DnsMessage();

                    var offset = 0;
                    reader.ReadBytes(message, ref offset);

                    offset = 0;
                    reader.WriteBytes(buffer, ref offset);
                }
            }
        }
    }
}
