namespace Ae.Dns.Client
{
    /// <summary>
    /// The options for <see cref="DnsRacerClient"/>.
    /// </summary>
    public sealed class DnsRacerClientOptions
    {
        /// <summary>
        /// The number of clients to randomly select and start queries for.
        /// If a fewer number of clients are supplied to the <see cref="DnsRacerClient"/>, 
        /// all clients will start queries.
        /// </summary>
        public int RandomClientQueries { get; set; } = 2;
    }
}
