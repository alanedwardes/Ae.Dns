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
        public DnsLabels(IEnumerable<string> strings) => _labels = strings?.ToArray() ?? throw new ArgumentNullException(nameof(strings));

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
        public override string ToString() => string.Join(".", LabelsNeverNull);

        /// <summary>
        /// Convert a string to <see cref="DnsLabels"/>.
        /// </summary>
        /// <param name="labels"></param>
        public static implicit operator DnsLabels(string labels) => new DnsLabels(labels);

        /// <summary>
        /// Convert an instance of <see cref="DnsLabels"/> to a string.
        /// </summary>
        /// <param name="labels"></param>
        public static implicit operator string(DnsLabels labels) => labels.ToString();
    }
}
