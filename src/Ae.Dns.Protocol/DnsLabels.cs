using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a set of labels.
    /// </summary>
    public readonly struct DnsLabels : IReadOnlyList<string>
    {
        /// <summary>
        /// Represents "empty" DNS labels.
        /// </summary>
        public static DnsLabels Empty = new DnsLabels();

        /// <summary>
        /// Construct a new <see cref="DnsLabels"/> instance using the specified labels.
        /// </summary>
        /// <param name="strings"></param>
        public DnsLabels(IReadOnlyList<string> strings) => _labels = strings ?? throw new ArgumentNullException(nameof(strings));

        /// <summary>
        /// Construct a new <see cref="DnsLabels"/> instance using joined labels.
        /// </summary>
        /// <param name="labels"></param>
        public DnsLabels(string labels) => _labels = labels?.Split('.') ?? throw new ArgumentNullException(nameof(labels));

        private readonly IReadOnlyList<string>? _labels;
        private IReadOnlyList<string> LabelsNeverNull => _labels ?? Array.Empty<string>();

        /// <inheritdoc/>
        public int Count => LabelsNeverNull.Count;

        /// <inheritdoc/>
        public string this[int index] => LabelsNeverNull[index];

        /// <inheritdoc/>
        public readonly IEnumerator<string> GetEnumerator() => LabelsNeverNull.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => LabelsNeverNull.GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => _labels == null ? "<none>" : string.Join(".", _labels);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DnsLabels labels && this == labels;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(LabelsNeverNull);

        /// <summary>
        /// Convert a string to <see cref="DnsLabels"/>.
        /// </summary>
        /// <param name="labels"></param>
        public static implicit operator DnsLabels(string labels) => new DnsLabels(labels);

        /// <summary>
        /// Convert an instance of <see cref="DnsLabels"/> to a string.
        /// </summary>
        /// <param name="labels"></param>
        public static implicit operator string(DnsLabels labels) => string.Join(".", labels.LabelsNeverNull);

        /// <summary>
        /// Test whether this instance equals the other instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(DnsLabels a, DnsLabels b) => a.SequenceEqual(b, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Test whether this instance does not equal the other instance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(DnsLabels a, DnsLabels b) => !(a == b);
    }
}
