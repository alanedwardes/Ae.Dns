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
            var answer1 = SampleDnsPackets.Answer2.ReadDnsAnswer();

            var bytes = answer1.WriteDnsAnswer().ToArray();

            var answer2 = bytes.ReadDnsAnswer();
        }
    }
}
