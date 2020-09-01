using System.Collections.Generic;

namespace Ae.Dns.Protocol
{
    public interface IByteArrayWriter
    {
        IEnumerable<IEnumerable<byte>> WriteBytes();
    }
}
