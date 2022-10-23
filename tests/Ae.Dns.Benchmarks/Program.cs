using Ae.Dns.Protocol;
using Ae.Dns.Tests;
using System;

namespace Ae.Dns.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)
        {
            Span<byte> buffer = new byte[65527];

            var answers = SampleDnsPackets.AnswerBatch1;

            for (var i = 0; i < 10_000; i++)
            {
                foreach (var answer in answers)
                {
                    var reader = new DnsMessage();

                    var offset = 0;
                    reader.ReadBytes(answer, ref offset);

                    offset = 0;
                    reader.WriteBytes(buffer, ref offset);
                }
            }
        }
    }
}
