using Ae.Dns.Client.Lookup;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ae.Dns.Tests.Client.Lookup
{
    public sealed class FileDnsLookupSourceTests : IDisposable
    {
        private readonly FileInfo _file = new(Path.GetTempFileName());

        public void Dispose() => _file.Delete();

        [Fact]
        public async Task TestReadHostsFile()
        {
            using (var sw = _file.CreateText())
            {
                sw.Write("one");
            }

            var source = new TestFileDnsLookupSource(_file);

            Assert.Equal("one", source.Contents);

            using (var sw = _file.AppendText())
            {
                sw.Write("two");
            }

            // Allow the file write event to fire
            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.Equal("onetwo", source.Contents);
        }

        private sealed class TestFileDnsLookupSource : FileDnsLookupSource
        {
            public TestFileDnsLookupSource(FileInfo file) : base(NullLogger<FileDnsLookupSource>.Instance, file)
            {
                ReloadFile();
            }

            public string Contents { get; private set; }

            protected override IEnumerable<(string hostname, IPAddress address)> LoadLookup(StreamReader sw)
            {
                Contents = sw.ReadToEnd();
                return Enumerable.Empty<(string hostname, IPAddress address)>();
            }
        }
    }
}
