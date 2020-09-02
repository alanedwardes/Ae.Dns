using System.Collections.Generic;

namespace Ae.Dns.Protocol
{
    public interface IDnsByteArrayWriter
    {
        IEnumerable<IEnumerable<byte>> WriteBytes();
    }
}
