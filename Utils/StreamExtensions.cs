using System.Runtime.CompilerServices;

namespace MvcChatSample.Utils
{
    public static class StreamExtensions
    {
        public static async IAsyncEnumerable<string> ReadLinesAsync(
            this Stream stream,
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line != null)
                    yield return line;
            }
        }
    }
}
