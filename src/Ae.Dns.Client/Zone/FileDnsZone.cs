﻿using Ae.Dns.Protocol.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ae.Dns.Client.Zone
{
    /// <summary>
    /// Represents a file-backed DNS zone.
    /// </summary>
    [Obsolete("Experimental: May change significantly in the future")]
    public sealed class FileDnsZone : IDnsZone
    {
        /// <summary>
        /// Construct a new zone, using the specified name (suffix) and file for persistence.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="file"></param>
        public FileDnsZone(string name, FileInfo file)
        {
            _name = name;
            _file = file;
            _ = ReloadZone(CancellationToken.None);
        }

        private readonly SemaphoreSlim _zoneLock = new SemaphoreSlim(1, 1);
        private readonly IList<DnsResourceRecord> _records = new List<DnsResourceRecord>();
        private readonly string _name;
        private readonly FileInfo _file;

        /// <inheritdoc/>
        public IEnumerable<DnsResourceRecord> Records => _records;

        private async Task ReloadZone(CancellationToken token)
        {
            await _zoneLock.WaitAsync(token);

            try
            {
                DeserializeZone();
            }
            finally
            {
                _zoneLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> AddRecords(IEnumerable<DnsResourceRecord> recordsToAdd, CancellationToken token = default)
        {
            if (recordsToAdd.Any(x => x.Host.Last() != _name))
            {
                return false;
            }

            await _zoneLock.WaitAsync(token);
            try
            {
                var recordsToRemove = _records.Where(x => recordsToAdd.Select(x => x.Host).Contains(x.Host));

                foreach (var recordToAdd in recordsToAdd)
                {
                    _records.Add(recordToAdd);
                }

                foreach(var recordToRemove in recordsToRemove)
                {
                    _records.Remove(recordToRemove);
                }

                SerializeZone();
            }
            finally
            {
                _zoneLock.Release();
            }

            return true;
        }

        private void DeserializeZone()
        {
            using var file = _file.Open(FileMode.OpenOrCreate, FileAccess.Read);
            using var reader = new BinaryReader(file);

            _records.Clear();

            for (int i = 0; i < reader.ReadInt32(); i++)
            {
                var length = reader.ReadInt32();
                var buffer = reader.ReadBytes(length);

                var record = new DnsResourceRecord();

                int offset = 0;
                record.ReadBytes(buffer, ref offset);

                _records.Add(record);
            }
        }

        private void SerializeZone()
        {
            using var file = _file.Open(FileMode.OpenOrCreate, FileAccess.Write);
            using var writer = new BinaryWriter(file);

            writer.Write(_records.Count);

            foreach (var record in _records)
            {
                var buffer = new byte[4096];

                int offset = 0;
                record.WriteBytes(buffer, ref offset);

                writer.Write(offset);
                writer.Write(buffer, 0, offset);
            }
        }
    }
}
