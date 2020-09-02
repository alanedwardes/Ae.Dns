namespace Ae.Dns.Protocol.Resources
{
    public interface IDnsResource : IDnsByteArrayWriter
    {
        void ReadBytes(byte[] bytes, ref int offset, int length);
    }
}
