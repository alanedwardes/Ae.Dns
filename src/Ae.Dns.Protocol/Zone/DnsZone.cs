using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Represents a simple DNS zone with no concurrency control.
    /// </summary>
    public sealed class DnsZone : IDnsZone
    {
        /// <inheritdoc/>
        public IList<DnsResourceRecord> Records { get; set; } = new List<DnsResourceRecord>();

        /// <inheritdoc/>
        public DnsLabels Origin { get; set; }

        /// <inheritdoc/>
        public TimeSpan? DefaultTtl { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Origin;

        /// <inheritdoc/>
        public Task<TResult> Update<TResult>(Func<TResult> modification) => Task.FromResult(modification());
    }
}
