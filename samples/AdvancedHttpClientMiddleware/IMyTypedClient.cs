public interface IMyTypedClient
{
    Task<HttpResponseMessage> GetGoogle(CancellationToken token);
}