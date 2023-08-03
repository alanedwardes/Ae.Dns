using Ae.Dns.Client.Lookup;
using System;
using System.IO;
using System.Linq;
using System.Net;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class HostsFileDnsLookupSourceTests : IDisposable
    {
        private readonly FileInfo _file = new FileInfo(Path.GetTempFileName());

        public void Dispose() => _file.Delete();

        [Fact]
        public void TestReadHostsFile()
        {
            using (var sw = _file.CreateText())
            {
                sw.WriteLine();
                sw.WriteLine("# test");
                sw.WriteLine();
                sw.WriteLine("127.0.0.1       localhost");
                sw.WriteLine();
            }

            var source = new HostsFileDnsLookupSource(_file);

            Assert.True(source.TryReverseLookup(IPAddress.Loopback, out var actualHostname));
            Assert.Equal("localhost", actualHostname.Single());

            Assert.True(source.TryForwardLookup("localhost", out var actualAddress));
            Assert.Equal(IPAddress.Loopback, actualAddress.Single());
        }
    }
}
