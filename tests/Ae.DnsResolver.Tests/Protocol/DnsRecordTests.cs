using Ae.DnsResolver.Protocol;
using System.Linq;
using Xunit;

namespace Ae.DnsResolver.Tests.Protocol
{
    public class DnsRecordTests
    {
        [Fact]
        public void TestDnsTextRecord()
        {
            int offset = 0;
            var answer1 = SampleDnsPackets.Answer2.ReadDnsAnswer(ref offset);

            var bytes = answer1.WriteDnsAnswer().ToArray();

            offset = 0;
            var answer2 = bytes.ReadDnsAnswer(ref offset);
        }
    }
}
