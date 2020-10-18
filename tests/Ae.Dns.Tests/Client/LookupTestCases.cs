using Ae.Dns.Protocol.Enums;
using System.Collections;
using System.Collections.Generic;

namespace Ae.Dns.Tests.Client
{
    public sealed class LookupTestCases : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "alanedwardes.com", DnsQueryType.A };
            yield return new object[] { "alanedwardes.com", DnsQueryType.AAAA };
            yield return new object[] { "alanedwardes.com", DnsQueryType.ANY };
            yield return new object[] { "google.com", DnsQueryType.A };
            yield return new object[] { "google.com", DnsQueryType.AAAA };
            yield return new object[] { "google.com", DnsQueryType.ANY };
            yield return new object[] { "cpsc.gov", DnsQueryType.A };
            yield return new object[] { "cpsc.gov", DnsQueryType.AAAA };
            yield return new object[] { "cpsc.gov", DnsQueryType.ANY };
            yield return new object[] { "gov.uk", DnsQueryType.A };
            yield return new object[] { "gov.uk", DnsQueryType.AAAA };
            yield return new object[] { "gov.uk", DnsQueryType.ANY };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
