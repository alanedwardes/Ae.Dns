using Ae.Dns.Client.Lookup;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class DhcpdConfigDnsLookupSourceTests : IDisposable
    {
        private readonly FileInfo _file = new FileInfo(Path.GetTempFileName());

        public void Dispose() => _file.Delete();

        [Fact]
        public void TestReadDhcpdLeasesFile()
        {
            using (var sw = _file.CreateText())
            {
                sw.WriteLine("option domain-name \"home\";");
                sw.WriteLine("option domain-name-servers 192.168.178.9;");
                sw.WriteLine("option routers 192.168.178.1;");
                sw.WriteLine();
                sw.WriteLine("subnet 192.168.178.0 netmask 255.255.255.0 {");
                sw.WriteLine("        range 192.168.178.64 192.168.178.254;");
                sw.WriteLine("}");
                sw.WriteLine();
                sw.WriteLine("host core {");
                sw.WriteLine("  hardware ethernet e4:5f:01:b3:32:4b;");
                sw.WriteLine("  fixed-address 192.168.178.9;");
                sw.WriteLine("}");
                sw.WriteLine();
                sw.WriteLine("host homeassistant {");
                sw.WriteLine("  hardware ethernet e4:5f:01:93:86:93;");
                sw.WriteLine("  fixed-address 192.168.178.11;");
                sw.WriteLine("}");
                sw.WriteLine();
            }

            var source = new DhcpdConfigDnsLookupSource(_file);

            Assert.False(source.TryReverseLookup(IPAddress.Parse("192.168.178.1"), out var _));
            Assert.False(source.TryReverseLookup(IPAddress.Broadcast, out var _));
            Assert.True(source.TryReverseLookup(IPAddress.Parse("192.168.178.9"), out var actualHostname));
            Assert.Equal("core", actualHostname.Single());

            Assert.False(source.TryForwardLookup("wibble", out var _));
            Assert.True(source.TryForwardLookup("cOrE", out var actualAddress));
            Assert.Equal(IPAddress.Parse("192.168.178.9"), actualAddress.Single());
        }
    }
}
