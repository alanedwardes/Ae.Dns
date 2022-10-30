using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Describes a set of DNS label strings, which is represented as raw memory.
    /// </summary>
    public readonly struct DnsLabels : IEquatable<DnsLabels>, IEnumerable<string>, IReadOnlyList<string>
    {
        /// <summary>
        /// Provides access to the raw array of entries.
        /// </summary>
        public readonly IReadOnlyList<ReadOnlyMemory<byte>> Labels;

        /// <summary>
        /// Create DNS labels from a set of raw memory.
        /// Note: This class doesn't understand DNS label compression.
        /// </summary>
        public DnsLabels(IReadOnlyList<ReadOnlyMemory<byte>> entries) => Labels = entries;

        /// <summary>
        /// Create DNS labels from a set of strings.
        /// </summary>
        /// <param name="entries"></param>
        public DnsLabels(IEnumerable<string> entries) => Labels = entries.Select(Encoding.ASCII.GetBytes).Select(x => new ReadOnlyMemory<byte>(x)).ToArray();

        private IEnumerable<string> AsStrings => Labels.Select(x => Encoding.ASCII.GetString(x.Span));

        public string AsHost => string.Join('.', AsStrings);

        public string AsString => string.Concat(AsStrings);

        public static implicit operator DnsLabels(ReadOnlyMemory<byte>[] entries) => new DnsLabels(entries);

        public static implicit operator DnsLabels(string[] strings) => new DnsLabels(strings);

        /// <inheritdoc/>
        public int Count => Labels.Count;

        /// <inheritdoc/>
        public override string ToString() => '"' + string.Join("\", \"", AsStrings) + '"';

        /// <inheritdoc/>
        public IEnumerator<string> GetEnumerator() => AsStrings.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public bool Equals(DnsLabels other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (!Labels[i].Span.SequenceEqual(other.Labels[i].Span))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public string this[int i] => Encoding.ASCII.GetString(Labels[i].Span);
    }
}

