using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ae.Dns.Protocol
{
    public readonly struct StringLabels : IEquatable<StringLabels>, IEnumerable<string>
    {
        public readonly ReadOnlyMemory<byte>[] Entries;

        public StringLabels(ReadOnlyMemory<byte>[] entries) => Entries = entries;

        public StringLabels(string[] entries) => Entries = entries.Select(Encoding.ASCII.GetBytes).Select(x => new ReadOnlyMemory<byte>(x)).ToArray();

        private IEnumerable<string> AsStrings => Entries.Select(x => Encoding.ASCII.GetString(x.Span));

        public string AsHost => string.Join('.', AsStrings);

        public string AsString => string.Concat(AsStrings);

        public override string ToString() => '"' + string.Join("\", \"", AsStrings) + '"';

        public IEnumerator<string> GetEnumerator() => AsStrings.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(StringLabels other)
        {
            if (other.Entries.Length == Entries.Length)
            {
                for (int i = 0; i < Entries.Length; i++)
                {
                    if (!Entries[i].Span.SequenceEqual(other.Entries[i].Span))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public static implicit operator StringLabels(ReadOnlyMemory<byte>[] entries) => new StringLabels(entries);

        public static implicit operator StringLabels(string[] strings) => new StringLabels(strings);

        public static implicit operator string[](StringLabels strings) => strings.AsStrings.ToArray();

        public string this[int i] => Encoding.ASCII.GetString(Entries[i].Span);
    }
}

