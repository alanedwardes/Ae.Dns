using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Protocol.Zone
{
    /// <summary>
    /// Represents a file-backed DNS zone.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class DnsZone : IDnsZone
    {
        private readonly List<DnsResourceRecord> _records = new List<DnsResourceRecord>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Construct a new <see cref="DnsZone"/> with no records.
        /// </summary>
        public DnsZone()
        {
        }

        /// <summary>
        /// Construct a new <see cref="DnsZone"/> with the specified records.
        /// </summary>
        /// <param name="records"></param>
        public DnsZone(IEnumerable<DnsResourceRecord> records)
        {
            _records = records.ToList();
        }

        /// <inheritdoc/>
        public IReadOnlyList<DnsResourceRecord> Records => _records;

        /// <inheritdoc/>
        public DnsLabels Origin { get; set; }

        /// <inheritdoc/>
        public TimeSpan DefaultTtl { get; set; }

        /// <inheritdoc/>
        public Func<IDnsZone, Task> ZoneUpdated { get; set; } = zone => Task.CompletedTask;

        /// <inheritdoc/>
        public async Task Update(Action<IList<DnsResourceRecord>> modification)
        {
            await _semaphore.WaitAsync();
            try
            {
                modification(_records);
                await ZoneUpdated(this);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public string SerializeZone()
        {
            var writer = new StringBuilder();
            writer.AppendLine($"$ORIGIN {Origin}.");
            writer.AppendLine($"$TTL {(int)DefaultTtl.TotalSeconds}");

            foreach (var record in Records)
            {
                writer.AppendLine(record.ToZone(this));
            }

            return writer.ToString();
        }

        /// <inheritdoc/>
        public void DeserializeZone(string zone)
        {
            _records.Clear();

            var reader = new StringReader(zone);

            string? spillage = null;
            string? line;
            bool spillover = false;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Split(';')[0];

                if (line.Contains("(") && !line.Contains(")"))
                {
                    spillover = true;
                }

                if (line.Contains(")"))
                {
                    line = spillage + line;
                    spillover = false;
                }

                if (spillover)
                {
                    spillage += line;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("$ORIGIN"))
                {
                    Origin = line.Substring("$ORIGIN".Length).Trim().Trim('.');
                    continue;
                }

                if (line.StartsWith("$TTL"))
                {
                    DefaultTtl = TimeSpan.FromSeconds(int.Parse(line.Substring("$TTL".Length)));
                    continue;
                }

                var record = new DnsResourceRecord();
                record.FromZone(this, line);
                _records.Add(record);
            }
            
        }

        /// <inheritdoc/>
        public string FromFormattedHost(string host)
        {
            if (host == "@")
            {
                return Origin;
            }
            else if (host.EndsWith("."))
            {
                return host.Substring(0, host.Length - 1);
            }
            else
            {
                return host + "." + Origin;
            }
        }

        /// <inheritdoc/>
        public string ToFormattedHost(string host)
        {
            if (host == Origin)
            {
                return "@";
            }
            else if (host.EndsWith(Origin))
            {
                return host.Substring(0, host.Length - Origin.ToString().Length - 1);
            }
            else
            {
                return host + '.';
            }
        }
    }
}
