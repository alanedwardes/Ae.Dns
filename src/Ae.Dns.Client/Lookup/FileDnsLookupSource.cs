using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// An abstract implementation of <see cref="IDnsLookupSource"/> which watches the specified
    /// file for changes, and calls <see cref="Rebuild"/>.
    /// </summary>
    public abstract class FileDnsLookupSource : IDnsLookupSource
    {
        private IDictionary<string, IPAddress> _hostsToAddresses = new Dictionary<string, IPAddress>();
        private IDictionary<IPAddress, string> _addressesToHosts = new Dictionary<IPAddress, string>();
        private readonly FileSystemWatcher _watcher;
        private readonly ILogger<FileDnsLookupSource> _logger;
        private readonly FileInfo _file;

        /// <summary>
        /// Construct a <see cref="FileDnsLookupSource"/> using the specified <see cref="FileInfo"/>.
        /// </summary>
        public FileDnsLookupSource(ILogger<FileDnsLookupSource> logger, FileInfo file)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException("File not found", file.FullName);
            }

            _watcher = new FileSystemWatcher(file.DirectoryName, file.Name)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFilechanged;
            _logger = logger;
            _file = file;
            Rebuild();
        }

        private void OnFilechanged(object sender, FileSystemEventArgs e) => RebuildWrapped();

        /// <summary>
        /// Load the lookup from the file.
        /// </summary>
        protected abstract IEnumerable<(string hostname, IPAddress address)> LoadLookup(StreamReader sr);

        private async Task RebuildWrapped()
        {
            try
            {
                await Rebuild();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Unable to rebuild lookup in response to file change event from {File}", _file);
            }
        }

        private async Task Rebuild()
        {
            using var sr = await ReadFileWithRetries();

            var hostsToAddresses = new Dictionary<string, IPAddress>();
            var addressesToHosts = new Dictionary<IPAddress, string>();

            foreach (var (hostname, address) in LoadLookup(sr))
            {
                hostsToAddresses[hostname.ToLowerInvariant()] = address;
                addressesToHosts[address] = hostname.ToLowerInvariant();
            }

            lock (_hostsToAddresses)
            lock (_addressesToHosts)
            {
                _hostsToAddresses = hostsToAddresses;
                _addressesToHosts = addressesToHosts;
            }

            _logger.LogInformation("Updated lookup tables with {ItemCount} items", _hostsToAddresses.Count);
        }

        private async Task<StreamReader> ReadFileWithRetries()
        {
            for (int attempt = 1; attempt < 5; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt));

                try
                {
                    return _file.OpenText();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to open file {File}, attempt {Attempt}", _file, attempt);
                }
            }

            throw new IOException($"Unable to access file {_file} after 5 attempts");
        }

        /// <inheritdoc/>
        public void Dispose() => _watcher.Dispose();

        /// <inheritdoc/>
        public bool TryForwardLookup(string hostname, out IPAddress address) => _hostsToAddresses.TryGetValue(hostname, out address);

        /// <inheritdoc/>
        public bool TryReverseLookup(IPAddress address, out string hostname) => _addressesToHosts.TryGetValue(address, out hostname);
    }
}
