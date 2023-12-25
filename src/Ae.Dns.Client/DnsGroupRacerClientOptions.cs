using System.Collections.Generic;
using Ae.Dns.Protocol;

namespace Ae.Dns.Client
{
    /// <summary>
    /// The options for <see cref="DnsRacerClient"/>.
    /// </summary>
    public sealed class DnsGroupRacerClientOptions
    {
        /// <summary>
        /// The number of groups from which to randomly select a client for queries.
        /// If a fewer number of groups are supplied to the <see cref="DnsGroupRacerClientOptions"/>, 
        /// all groups will start queries.
        /// </summary>
        public int RandomGroupQueries { get; set; } = 2;

        /// <summary>
        /// A dictionary of DNS clients grouped by an arbitrary identifier.
        /// This can be used to group DNS clients of the same provider, for example.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<IDnsClient>> DnsClientGroups { get; set; } = new Dictionary<string, IReadOnlyList<IDnsClient>>();
    }
}
