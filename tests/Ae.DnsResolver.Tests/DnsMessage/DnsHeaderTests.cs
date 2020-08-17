using Ae.DnsResolver.Protocol;
using Xunit;

namespace Ae.DnsResolver.Tests.DnsMessage
{
    public class DnsHeaderTests
    {
        [Fact]
        public void TestDnsHeader()
        {
            var header = new DnsHeader();

            header.OperationCode = DnsOperationCode.STATUS;





            Assert.Equal(DnsOperationCode.STATUS, header.OperationCode);
        }
    }
}
