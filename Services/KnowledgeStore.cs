using Microsoft.AspNetCore.Hosting;
using System.Text;

public class KnowledgeStore
{
    private readonly string _knowledgePath;

    public KnowledgeStore(IWebHostEnvironment env)
    {
        _knowledgePath = Path.Combine(env.ContentRootPath, "Knowledge");
        Console.WriteLine($"[KnowledgeStore] Path = {_knowledgePath}");
    }

    public async Task SaveAsync(string title, string content)
    {
        Directory.CreateDirectory(_knowledgePath);

        var file = Path.Combine(_knowledgePath, $"{title}.md");
        await File.WriteAllTextAsync(file, content, Encoding.UTF8);
    }

    public string ReadAll()
    {
        if (!Directory.Exists(_knowledgePath))
            return string.Empty;

        var files = Directory.GetFiles(_knowledgePath, "*.md");

        if (files.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var file in files)
        {
            sb.AppendLine(File.ReadAllText(file));
            sb.AppendLine("\n");
        }

        return sb.ToString();
    }

    public IEnumerable<string> ListTitles()
    {
        if (!Directory.Exists(_knowledgePath))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(_knowledgePath, "*.md")
                        .Select(Path.GetFileNameWithoutExtension);
    }
}
