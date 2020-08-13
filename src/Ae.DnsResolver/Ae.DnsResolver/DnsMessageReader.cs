using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Ae.DnsResolver
{
    public enum Qtype : ushort
    {
        A = 0x0001,
        NS = 0x0002,
        MD = 0x0003,
        MF = 0x0004,
        CNAME = 0x0005,
        SOA = 0x0006,
        MB = 0x0007,
        MG = 0x0008,
        MR = 0x0009,
        NULL = 0x000a,
        WKS = 0x000b,
        PTR = 0x000c,
        HINFO = 0x000d,
        MINFO = 0x000e,
        MX = 0x000f,
        TEXT = 0x0010,
        RP = 0x0011,
        AFSDB = 0x0012,
        X25 = 0x0013,
        ISDN = 0x0014,
        RT = 0x0015,
        NSAP = 0x0016,
        NSAPPTR = 0x0017,
        SIG = 0x0018,
        KEY = 0x0019,
        PX = 0x001a,
        GPOS = 0x001b,
        AAAA = 0x001c,
        LOC = 0x001d,
        NXT = 0x001e,
        EID = 0x001f,
        NIMLOC = 0x0020,
        SRV = 0x0021,
        ATMA = 0x0022,
        NAPTR = 0x0023,
        KX = 0x0024,
        CERT = 0x0025,
        A6 = 0x0026,
        DNAME = 0x0027,
        SINK = 0x0028,
        OPT = 0x0029,
        DS = 0x002B,
        RRSIG = 0x002E,
        NSEC = 0x002F,
        DNSKEY = 0x0030,
        DHCID = 0x0031,
        UINFO = 0x0064,
        UID = 0x0065,
        GID = 0x0066,
        UNSPEC = 0x0067,
        ADDRS = 0x00f8,
        TKEY = 0x00f9,
        TSIG = 0x00fa,
        IXFR = 0x00fb,
        AXFR = 0x00fc,
        MAILB = 0x00fd,
        MAILA = 0x00fe,
        ALL = 0x00ff,
        ANY = 0x00ff,
        WINS = 0xff01,
        WINSR = 0xff02,
        NBSTAT = WINSR
    }

    public enum Qclass : short
    {
        None = 0,
        IN = 1,
        CS = 2,
        CH = 3,
        HS = 4
    }

    public abstract class DnsMessage
    {
        public short Id;
        public short Header;
        public short Qdcount;
        public short Ancount;
        public short Nscount;
        public short Arcount;

        public string[] Labels;
        public Qtype Qtype;
        public Qclass Qclass;
    }

    public static class EndianExtensions
    {
        private static short SwapEndian(this short val)
        {
            return BitConverter.IsLittleEndian ? (short)((val << 8) | (val >> 8)) : val;
        }

        private static ushort SwapEndian(this ushort val)
        {
            return BitConverter.IsLittleEndian ? (ushort)((val << 8) | (val >> 8)) : val;
        }

        private static uint SwapEndian(this uint val)
        {
            return BitConverter.IsLittleEndian ? (uint)((val << 16) | (val >> 16)) : val;
        }

        public static byte ReadByte(this byte[] bytes, ref int offset)
        {
            return bytes[offset++];
        }

        public static short ReadInt16(this byte[] bytes, ref int offset)
        {
            offset += sizeof(short);
            return BitConverter.ToInt16(bytes, offset - sizeof(short)).SwapEndian();
        }

        public static ushort ReadUInt16(this byte[] bytes, ref int offset)
        {
            offset += sizeof(ushort);
            return BitConverter.ToUInt16(bytes, offset - sizeof(ushort)).SwapEndian();
        }

        public static uint ReadUInt32(this byte[] bytes, ref int offset)
        {
            offset += sizeof(uint);
            return BitConverter.ToUInt32(bytes, offset - sizeof(uint)).SwapEndian();
        }

        public static byte[] ReadBytes(this byte[] bytes, int length, ref int offset)
        {
            var data = new byte[length];
            Array.Copy(bytes, offset, data, 0, length);
            offset += length;
            return data;
        }

        public static bool IsBitSet(this byte octet, int position)
        {
            // Endianness fix
            //octet = (byte)((octet << 4) | (octet >> 4));
            return (octet & (1 << position)) != 0;
            //return ((octet >> position) & 1) != 0;
        }

        //public static string[] ReadString2(this byte[] bytes, ref int offset)
        //{
        //    var parts = new List<string>();

        //    while (true)
        //    {
        //        var octet = bytes[offset];
        //        if (octet == 0)
        //        {
        //            offset++;
        //            break;
        //        }

        //        if ((octet & 0xC0) == 0xC0)
        //        {
        //            var pointer = (int)bytes[offset + 1];
        //            parts.AddRange(DnsMessageReader.ReadName2(bytes, ref pointer));
        //            offset += 2;

        //            if (bytes[offset] == 0)
        //            {
        //                break;
        //            }
        //        }
        //        else
        //        {
        //            offset++;
        //            parts.Add(DnsMessageReader.ReadSingleString(bytes, octet, ref offset));
        //        }
        //    }

        //    return parts.ToArray();
        //}

        public static string[] ReadString(this byte[] bytes, ref int offset)
        {
            var parts = new List<string>();

            int compressionOffset = -1;
            while (true)
            {
                // get segment length or detect termination of segments
                int segmentLength = bytes[offset];

                // compressed name
                if ((segmentLength & 0xC0) == 0xC0)
                {
                    offset++;
                    if (compressionOffset == -1)
                    {
                        // only record origin, and follow all pointers thereafter
                        compressionOffset = offset;
                    }

                    //if (segmentLength != 192)
                    {
                        var mask = (1 << 14) - 1;
                        var pointer = ((ushort)(segmentLength + (bytes[offset] << 8))).SwapEndian() & mask;
                        offset = pointer;
                        segmentLength = pointer;

                        if (offset != bytes[offset])
                        {
                            Debugger.Break();
                        }
                    }
                    //else
                    {
                        // move pointer to compression segment
                        //offset = bytes[offset];
                        //segmentLength = bytes[offset];
                    }
                }

                if (segmentLength == 0x00)
                {
                    if (compressionOffset != -1)
                    {
                        offset = compressionOffset;
                    }
                    // move past end of name \0
                    offset++;
                    break;
                }

                // move pass length and get segment text
                offset++;
                parts.Add(Encoding.ASCII.GetString(bytes, offset, segmentLength));
                offset += segmentLength;
            }



            return parts.ToArray();
        }
    }

    public class DnsRequestMessage : DnsMessage
    {
        public override string ToString() => string.Format("REQUEST: Domain: {0}, type: {1}, class: {2}", string.Join(".", Labels), Qtype, Qclass);
    }

    public class DnsResponseMessage : DnsMessage
    {
        public DnsResourceRecord[] Records;

        public override string ToString() => string.Format("RESPONSE: Domain: {0}, type: {1}, class: {2}, records: {3}", string.Join(".", Labels), Qtype, Qclass, Records.Length);
    }

    public class DnsResourceRecord
    {
        public string[] Name;
        public Qtype Type;
        public Qclass Class;
        public TimeSpan Ttl;
        public byte[] Data;

        public string DataAsString => Encoding.ASCII.GetString(Data);
        public IPAddress DataAsIp => new IPAddress(Data);
    }

    public static class DnsMessageReader
    {
        public static DnsMessage ReadDnsMessage(byte[] bytes)
        {
            var result = new DnsRequestMessage();
            var offset = 0;
            ReadDnsMessage(bytes, result, ref offset);
            return result;
        }

        public static DnsResponseMessage ReadDnsResponse(byte[] bytes)
        {
            var result = new DnsResponseMessage();
            var offset = 0;
            ReadDnsMessage(bytes, result, ref offset);

            var records = new List<DnsResourceRecord>();

            for (var i = 0; i < result.Ancount; i++)
            {
                records.Add(ReadResourceRecord(bytes, ref offset));
            }

            result.Records = records.ToArray();

            return result;
        }

        private static DnsResourceRecord ReadResourceRecord(byte[] bytes, ref int offset)
        {
            var test = bytes.Select(x => (char)x).ToArray();

            //var originalOffset = offset;
            var resourceName = bytes.ReadString(ref offset);
            //var expectedOffset = offset;
            //offset = originalOffset;

            //var resourceName2 = bytes.ReadString2(ref offset);

            //Debug.Assert(offset == expectedOffset);
            //Debug.Assert(resourceName.SequenceEqual(resourceName2));

            var resourceType = (Qtype)bytes.ReadUInt16(ref offset);
            var resourceClass = (Qclass)bytes.ReadUInt16(ref offset);
            var ttl = bytes.ReadUInt32(ref offset);
            var rdlength = bytes.ReadUInt16(ref offset);

            byte[] rdata = bytes.ReadBytes(rdlength, ref offset);

            if (resourceType == Qtype.CNAME)
            {
                rdata = bytes.ReadString(ref offset).SelectMany(x => x.ToArray()).Select(x => (byte)x).ToArray();
            }

            return new DnsResourceRecord
            {
                Name = resourceName,
                Type = resourceType,
                Class = resourceClass,
                Ttl = TimeSpan.FromSeconds(ttl),
                Data = rdata
            };
        }

        public static string[] ReadName2(byte[] bytes, ref int offset)
        {
            var parts = new List<string>();

            var octets = bytes.ReadByte(ref offset);
            while (octets > 0 && (octets & 0xC0) != 0xC0)
            {
                parts.Add(ReadSingleString(bytes, octets, ref offset));
                octets = bytes.ReadByte(ref offset);
            }

            return parts.ToArray();
        }

        public static string ReadSingleString(byte[] bytes, int length, ref int offset)
        {
            return Encoding.ASCII.GetString(bytes.ReadBytes(length, ref offset));
        }

        private static DnsMessage ReadDnsMessage(byte[] bytes, DnsMessage result, ref int offset)
        {
            result.Id = bytes.ReadInt16(ref offset);
            result.Header = bytes.ReadInt16(ref offset);
            result.Qdcount = bytes.ReadInt16(ref offset);
            result.Ancount = bytes.ReadInt16(ref offset);
            result.Nscount = bytes.ReadInt16(ref offset);
            result.Arcount = bytes.ReadInt16(ref offset);
            result.Labels = ReadName2(bytes, ref offset);
            result.Qtype = (Qtype)bytes.ReadInt16(ref offset);
            result.Qclass = (Qclass)bytes.ReadInt16(ref offset);
            return result;
        }
    }
}
