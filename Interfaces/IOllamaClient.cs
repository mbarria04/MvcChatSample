namespace MvcChatSample.Interfaces
{
    public interface IOllamaClient
    {
        IAsyncEnumerable<string> StreamAsync(
            string question,
            string context,
            CancellationToken ct);
    }
}
