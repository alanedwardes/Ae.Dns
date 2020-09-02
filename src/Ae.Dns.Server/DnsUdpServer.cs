using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Server
{
    public sealed class DnsUdpServer
    {
        private readonly ILogger<DnsUdpServer> _logger;
        private readonly UdpClient _listener;
        private readonly IDnsClient _dnsClient;
        private readonly IDnsFilter _dnsFilter;

        public DnsUdpServer(ILogger<DnsUdpServer> logger, UdpClient listener, IDnsClient dnsClient, IDnsFilter dnsFilter)
        {
            _logger = logger;
            _listener = listener;
            _dnsClient = dnsClient;
            _dnsFilter = dnsFilter;
        }

        public async Task Recieve(CancellationToken token)
        {
            _logger.LogInformation("Server now listening");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _listener.ReceiveAsync();
                    Respond(result, token);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error with incoming connection");
                }
            }
        }

        private DnsHeader CreateNullHeader(DnsHeader request)
        {
            return new DnsHeader
            {
                Id = request.Id,
                ResponseCode = DnsResponseCode.NXDomain,
                IsQueryResponse = true,
                RecursionAvailable = true,
                RecusionDesired = request.RecusionDesired,
                Host = request.Host,
                QueryClass = request.QueryClass,
                QuestionCount = request.QuestionCount,
                QueryType = request.QueryType
            };
        }

        private async Task<DnsAnswer> GetAnswer(DnsHeader message, CancellationToken token)
        {
            if (_dnsFilter.IsPermitted(message))
            {
                return await _dnsClient.Query(message, token);
            }
            else
            {
                _logger.LogTrace("DNS query blocked for {Domain}", message.Host);
                return new DnsAnswer { Header = CreateNullHeader(message) };
            }
        }

        private async void Respond(UdpReceiveResult query, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            DnsHeader message;
            try
            {
                message = query.Buffer.FromBytes<DnsHeader>();
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to parse incoming packet: {0}", query.Buffer.ToDebugString());
                return;
            }

            DnsAnswer answer;
            try
            {
                answer = await GetAnswer(message, token);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to resolve {0}", message);
                return;
            }

            var answerBytes = answer.ToBytes().ToArray();

            try
            {
                await _listener.SendAsync(answerBytes, answerBytes.Length, query.RemoteEndPoint);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to send back response to {0}", query.RemoteEndPoint);
                return;
            }

            _logger.LogTrace("Responded to DNS request for {Domain} in {ResponseTime}", message.Host, stopwatch.Elapsed.TotalSeconds);
        }
    }
}
