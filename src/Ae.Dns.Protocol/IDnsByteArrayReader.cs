namespace Ae.Dns.Protocol
{
    public interface IDnsByteArrayReader
    {
        void ReadBytes(byte[] bytes, ref int offset);
    }
}
