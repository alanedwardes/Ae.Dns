using Ae.Dns.Protocol;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Timer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Metrics.InfluxDb
{
    /// <summary>
    /// A client which logs metrics aganst DNS responses.
    /// </summary>
    public sealed class DnsInfluxDbMetricsClient : IDnsClient
    {
        private readonly IMetrics _metrics;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsInfluxDbMetricsClient"/> using the specified <see cref="IDnsClient"/>.
        /// </summary>
        public DnsInfluxDbMetricsClient(IMetrics metrics, IDnsClient dnsClient)
        {
            _metrics = metrics;
            _dnsClient = dnsClient;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public async Task<DnsMessage> Query(DnsMessage query, CancellationToken token = default)
        {
            var sw = Stopwatch.StartNew();

            var tags = new Dictionary<string, string>
            {
                { "Host", query.Header.Host}
            };

            DnsMessage answer;
            try
            {
                answer = await _dnsClient.Query(query, token);
            }
            catch
            {
                _metrics.Measure.Counter.Increment(new CounterOptions
                {
                    Name = "Exceptions",
                    MeasurementUnit = Unit.Errors,
                    Tags = MetricTags.Concat(MetricTags.Empty, tags)
                });
                throw;
            }
            finally
            {
                sw.Stop();
            }

            tags.Add("ResponseCode", answer.Header.ResponseCode.ToString());

            bool isCached = answer.Header.Tags.TryGetValue("IsCached", out var isCachedObject) && isCachedObject is bool isCachedResult && isCachedResult;
            tags.Add("IsCached", isCached.ToString());

            bool isPrefetch = answer.Header.Tags.TryGetValue("IsPrefetch", out var isPrefetchObject) && isPrefetchObject is bool isPrefetchResult && isPrefetchResult;
            tags.Add("IsPrefetch", isPrefetch.ToString());

            string resolver = answer.Header.Tags.TryGetValue("Resolver", out var resolverObject) && resolverObject is IDnsClient dnsClient ? dnsClient.ToString() : "Unknown";
            tags.Add("Resolver", resolver);

            _metrics.Measure.Timer.Time(new TimerOptions
            {
                Name = "AnswerTime",
                DurationUnit = TimeUnit.Milliseconds,
                MeasurementUnit = Unit.Calls,
                RateUnit = TimeUnit.Milliseconds,
                Tags = MetricTags.Concat(MetricTags.Empty, tags)
            }, sw.ElapsedMilliseconds);

            _metrics.Measure.Counter.Increment(new CounterOptions
            {
                Name = "AnswerResult",
                MeasurementUnit = Unit.Results,
                Tags = MetricTags.Concat(MetricTags.Empty, tags)
            });

            return answer;
        }
    }
}
