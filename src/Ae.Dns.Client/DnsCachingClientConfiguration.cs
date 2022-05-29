using System;

namespace Ae.Dns.Client
{
    /// <summary>
    /// Allows configuration of the <see cref="DnsCachingClient"/>
    /// </summary>
    public sealed class DnsCachingClientConfiguration
    {
        /// <summary>
        /// Return stale cache entries for the specified time.
        /// The entry will be refreshed in the background so it is
        /// fresh again for the next caller.
        /// </summary>
        public TimeSpan AdditionalTimeToLive { get; set; } = TimeSpan.Zero;
    }
}
