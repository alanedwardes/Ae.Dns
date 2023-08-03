﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Ae.Dns.Client.Lookup
{
    /// <summary>
    /// An abstract implementation of <see cref="IDnsLookupSource"/> which watches the specified
    /// file for changes, and calls <see cref="ReloadFile"/>.
    /// </summary>
    public abstract class FileDnsLookupSource : IDnsLookupSource
    {
        private readonly object _swapLock = new object();
        private IDictionary<string, IList<IPAddress>> _hostsToAddresses = new Dictionary<string, IList<IPAddress>>();
        private IDictionary<IPAddress, IList<string>> _addressesToHosts = new Dictionary<IPAddress, IList<string>>();
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
        }

        /// <summary>
        /// Construct a <see cref="FileDnsLookupSource"/> using the specified <see cref="FileInfo"/>.
        /// </summary>
        public FileDnsLookupSource(FileInfo file)
            : this(NullLogger<FileDnsLookupSource>.Instance, file)
        {
        }

        private void OnFilechanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                ReloadFile();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Unable to rebuild lookup in response to file change event from {File}", _file);
            }
        }

        /// <summary>
        /// Load the lookup from the file.
        /// </summary>
        protected abstract IEnumerable<(string hostname, IPAddress address)> LoadLookup(StreamReader sr);

        /// <summary>
        /// Reload the lookup from the file.
        /// </summary>
        protected void ReloadFile()
        {
            using var sr = ReadFileWithRetries();

            var hostsToAddresses = new Dictionary<string, IList<IPAddress>>(StringComparer.InvariantCultureIgnoreCase);
            var addressesToHosts = new Dictionary<IPAddress, IList<string>>();

            foreach (var (hostname, address) in LoadLookup(sr))
            {
                if (hostsToAddresses.ContainsKey(hostname))
                {
                    hostsToAddresses[hostname].Add(address);
                }
                else
                {
                    hostsToAddresses[hostname] = new List<IPAddress> { address };
                }

                if (addressesToHosts.ContainsKey(address))
                {
                    addressesToHosts[address].Add(hostname);
                }
                else
                {
                    addressesToHosts[address] = new List<string> { hostname };
                }
            }

            lock (_swapLock)
            {
                _hostsToAddresses = hostsToAddresses;
                _addressesToHosts = addressesToHosts;
            }

            _logger.LogInformation("Updated lookup tables with {ItemCount} items", _hostsToAddresses.Count);
        }

        private StreamReader ReadFileWithRetries()
        {
            for (int attempt = 1; attempt < 5; attempt++)
            {
                try
                {
                    return _file.OpenText();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to open file {File}, attempt {Attempt}", _file, attempt);
                }

                Thread.Sleep(TimeSpan.FromSeconds(attempt));
            }

            throw new IOException($"Unable to access file {_file} after 5 attempts");
        }

        /// <inheritdoc/>
        public void Dispose() => _watcher.Dispose();

        /// <inheritdoc/>
        public bool TryForwardLookup(string hostname, out IList<IPAddress> addresses)
        {
            return _hostsToAddresses.TryGetValue(hostname, out addresses);
        }

        /// <inheritdoc/>
        public bool TryReverseLookup(IPAddress address, out IList<string> hostnames)
        {
            return _addressesToHosts.TryGetValue(address, out hostnames);
        }

        /// <inheritdoc/>
        public override string ToString() => _file.Name;
    }
}
