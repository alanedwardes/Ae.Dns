namespace Ae.Dns.Protocol.Records
{
    public interface IDnsResource : IDnsByteArrayWriter
    {
        void ReadBytes(byte[] bytes, ref int offset, int length);
    }
}
