using Ae.Dns.Protocol.Zone;
using System.Collections.Generic;

namespace Ae.Dns.Console
{
    public sealed class ZoneRegistry
    {
        public sealed class ZoneEntry
        {
            public required SingleWriterDnsZone Zone { get; init; }
            public required string FilePath { get; init; }
        }

        public IReadOnlyList<ZoneEntry> Zones { get; }

        public ZoneRegistry(IReadOnlyList<ZoneEntry> zones)
        {
            Zones = zones;
        }
    }
}
