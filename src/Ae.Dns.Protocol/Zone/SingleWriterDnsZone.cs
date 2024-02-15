using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Describes an <see cref="IDnsZone"/> implementation which uses a <see cref="SemaphoreSlim"/> internally to ensure only one caller can call the <see cref="Update{TResult}(Func{TResult})"/> method at any one time.
    /// </summary>
    public sealed class SingleWriterDnsZone : IDnsZone
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <inheritdoc/>
        public IList<DnsResourceRecord> Records { get; set; } = new List<DnsResourceRecord>();

        /// <inheritdoc/>
        public DnsLabels Origin { get; set; }

        /// <inheritdoc/>
        public TimeSpan? DefaultTtl { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Origin;

        /// <summary>
        /// A delegate to call from within the Update lock after changes have been made.
        /// </summary>
        public Func<IDnsZone, Task> ZoneUpdated { get; set; } = zone => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task<TResult> Update<TResult>(Func<TResult> modification)
        {
            await _semaphore.WaitAsync();
            try
            {
                var result = modification();
                await ZoneUpdated(this);
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
