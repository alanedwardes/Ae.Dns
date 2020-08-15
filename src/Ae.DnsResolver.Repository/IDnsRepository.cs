using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ae.DnsResolver.Repository
{
    public interface IDnsRepository
    {
        Task<byte[]> Resolve(byte[] query);
    }

    public interface IDnsFilter
    {
        public bool IsPermitted(string domain);
    }

    public sealed class DnsSetFilter : IDnsFilter
    {
        private readonly ISet<string> _domains;

        public DnsSetFilter(ISet<string> domains)
        {
            _domains = domains;
        }

        public bool IsPermitted(string domain)
        {
            return !_domains.Contains(domain);
        }

        public static async Task<IDnsFilter> CrateFromRemoteHostsFile(string hostsFileUrl)
        {
            var set = new HashSet<string>();

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStreamAsync(hostsFileUrl);
                using (var sr = new StreamReader(response))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        if (line.Contains(" "))
                        {
                            var domain = line.Replace("0.0.0.0", string.Empty).Trim();
                            set.Add(domain);
                        }
                        else
                        {
                            set.Add(line.Trim());
                        }
                    }
                }
            }

            return new DnsSetFilter(set);
        }
    }
}
