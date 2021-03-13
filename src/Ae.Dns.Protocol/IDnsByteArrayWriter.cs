using System.Collections.Generic;

namespace Ae.Dns.Protocol
{
    /// <summary>
    /// Represents a class which can write its contents to an enumerable of bytes.
    /// </summary>
    public interface IDnsByteArrayWriter
    {
        /// <summary>
        /// Writes the contents of this class to the specified nested <see cref="IEnumerable{T}"/> of <see cref="byte"/>.
        /// </summary>
        /// <returns>A nested <see cref="IEnumerable{T}"/> of <see cref="byte"/>.</returns>
        IEnumerable<IEnumerable<byte>> WriteBytes();
    }
}
