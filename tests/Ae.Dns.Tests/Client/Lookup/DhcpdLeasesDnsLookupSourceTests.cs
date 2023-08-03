using Ae.Dns.Client.Lookup;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class DhcpdLeasesDnsLookupSourceTests : IDisposable
    {
        private readonly FileInfo _file = new FileInfo(Path.GetTempFileName());

        public void Dispose() => _file.Delete();

        [Fact]
        public void TestReadDhcpdLeasesFile()
        {
            using (var sw = _file.CreateText())
            {
                sw.WriteLine("# The format of this file is documented in the dhcpd.leases(5) manual page.");
                sw.WriteLine("# This lease file was written by isc-dhcp-4.4.1");
                sw.WriteLine();
                sw.WriteLine("# authoring-byte-order entry is generated, DO NOT DELETE");
                sw.WriteLine("authoring-byte-order little-endian;");
                sw.WriteLine();
                sw.WriteLine("lease 192.168.178.249 {");
                sw.WriteLine("  starts 3 2022/08/17 20:34:17;");
                sw.WriteLine("  ends 4 2022/08/18 08:34:17;");
                sw.WriteLine("  tstp 4 2022/08/18 08:34:17;");
                sw.WriteLine("  cltt 3 2022/08/17 20:34:17;");
                sw.WriteLine("  binding state free;");
                sw.WriteLine("  hardware ethernet 6e:a4:61:73:d4:72;");
                sw.WriteLine("  uid \"\001\\216\\244 as\\324r\";");
                sw.WriteLine("}");
                sw.WriteLine("lease 192.168.178.254 {");
                sw.WriteLine("  starts 6 2022/08/20 12:02:04;");
                sw.WriteLine("  ends 0 2022/08/21 00:02:04;");
                sw.WriteLine("  tstp 0 2022/08/21 00:02:04;");
                sw.WriteLine("  cltt 6 2022/08/20 14:49:11;");
                sw.WriteLine("  binding state free;");
                sw.WriteLine("  next binding state free;");
                sw.WriteLine("  rewind binding state free;");
                sw.WriteLine("  hardware ethernet 2a:5b:f1:c7:a9:9f;");
                sw.WriteLine("  uid \"\001\\032[\\361\\307\\251\\237\";");
                sw.WriteLine("  set vendor-class-identifier = \"android-dhcp-10\";");
                sw.WriteLine("  client-hostname \"OnePlus5\";");
                sw.WriteLine("}");
                sw.WriteLine();
            }

            var source = new DhcpdLeasesDnsLookupSource(_file);

            Assert.False(source.TryReverseLookup(IPAddress.Parse("192.168.178.249"), out var _));
            Assert.False(source.TryReverseLookup(IPAddress.Broadcast, out var _));
            Assert.True(source.TryReverseLookup(IPAddress.Parse("192.168.178.254"), out var actualHostname));
            Assert.Equal("OnePlus5", actualHostname.Single());

            Assert.False(source.TryForwardLookup("wibble", out var _));
            Assert.True(source.TryForwardLookup("oneplus5", out var actualAddress));
            Assert.Equal(IPAddress.Parse("192.168.178.254"), actualAddress.Single());
        }
    }
}
