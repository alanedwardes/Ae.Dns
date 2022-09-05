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
    public sealed class DnsAppMetricsClient : IDnsClient
    {
        private readonly IMetrics _metrics;
        private readonly IDnsClient _dnsClient;

        /// <summary>
        /// Construct a new <see cref="DnsAppMetricsClient"/> using the specified <see cref="IDnsClient"/>.
        /// </summary>
        public DnsAppMetricsClient(IMetrics metrics, IDnsClient dnsClient)
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
                    MeasurementUnit = Unit.Errors
                });
                throw;
            }
            finally
            {
                sw.Stop();
            }

            var tags = new Dictionary<string, string>
            {
                { "ResponseCode", answer.Header.ResponseCode.ToString() },
                { "QueryType", answer.Header.QueryType.ToString() }
            };

            if (answer.Header.Tags.TryGetValue("IsCached", out var isCached))
            {
                tags.Add("IsCached", isCached.ToString());
            }

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
