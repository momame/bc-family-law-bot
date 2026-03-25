using System.Net.Http.Json;
using System.Text.Json;
using BCFamilyLawBot.Models;

namespace BCFamilyLawBot.Services;

public class DocumentIngestionService
{
    private readonly string _token;
    private readonly string _embeddingModel;
    private readonly string _endpoint;
    private readonly string _dataPath;
    private readonly List<DocumentChunk> _chunks = new();

    public DocumentIngestionService(
        string token, string embeddingModel, string endpoint, string dataPath)
    {
        _token = token;
        _embeddingModel = embeddingModel;
        _endpoint = endpoint;
        _dataPath = dataPath;
    }

    public async Task IngestAsync()
    {
        if (!Directory.Exists(_dataPath))
        {
            Console.WriteLine($"Warning: Data directory not found: {_dataPath}");
            return;
        }

        var files = Directory.GetFiles(_dataPath, "*.md");
        Console.WriteLine($"Found {files.Length} markdown files to ingest.");

        var allChunks = new List<(string text, string source, string formName)>();

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var fileName = Path.GetFileNameWithoutExtension(file);
            var chunks = ChunkText(content, maxWords: 200, overlapWords: 40); // chunk size of ~200 words with 40 words overlap

            foreach (var chunk in chunks)
                allChunks.Add((chunk, fileName, ExtractFormName(fileName)));
        }

        Console.WriteLine($"Created {allChunks.Count} chunks. Generating embeddings...");

        int batchSize = 8;
        int chunkIndex = 0;

        for (int i = 0; i < allChunks.Count; i += batchSize)
        {
            var batch = allChunks.Skip(i).Take(batchSize).ToList();
            var texts = batch.Select(c => c.text).ToList();
            var embeddings = await GetEmbeddingsAsync(texts);

            for (int j = 0; j < batch.Count; j++)
            {
                _chunks.Add(new DocumentChunk
                {
                    Id = $"chunk-{chunkIndex++}",
                    Text = batch[j].text,
                    Source = batch[j].source,
                    FormName = batch[j].formName,
                    Embedding = embeddings[j]
                });
            }

            if (i + batchSize < allChunks.Count)
                await Task.Delay(1200);
        }

        Console.WriteLine($"Ingested {chunkIndex} chunks into vector store.");
    }

    public Task<List<DocumentChunk>> SearchAsync(string query, int topK = 5)
    {
        return SearchAsyncInternal(query, topK);
    }

    private async Task<List<DocumentChunk>> SearchAsyncInternal(string query, int topK)
    {
        var queryEmbedding = (await GetEmbeddingsAsync(new List<string> { query }))[0];

        // Cosine similarity search
        var results = _chunks
            .Select(c => new { Chunk = c, Score = CosineSimilarity(queryEmbedding, c.Embedding) })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();

        return results;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }

    private async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");

        var body = new { input = texts, model = _embeddingModel };

        var response = await client.PostAsJsonAsync(
            $"{_endpoint}/v1/embeddings", body);

        if (!response.IsSuccessStatusCode)
        {
            response = await client.PostAsJsonAsync(
                $"{_endpoint}/openai/deployments/{_embeddingModel}/embeddings?api-version=2024-02-01",
                body);
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray())
            .ToList();
    }

    private static List<string> ChunkText(string text, int maxWords, int overlapWords)
    {
        var chunks = new List<string>();
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" },
            StringSplitOptions.RemoveEmptyEntries);

        var current = "";
        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if ((current + " " + trimmed).Split(' ',
                StringSplitOptions.RemoveEmptyEntries).Length > maxWords)
            {
                if (!string.IsNullOrWhiteSpace(current))
                {
                    chunks.Add(current.Trim());
                    var words = current.Split(' ',
                        StringSplitOptions.RemoveEmptyEntries);
                    current = words.Length > overlapWords
                        ? string.Join(' ', words.Skip(words.Length - overlapWords))
                        : "";
                }
            }
            current += (string.IsNullOrWhiteSpace(current) ? "" : "\n\n")
                + trimmed;
        }

        if (!string.IsNullOrWhiteSpace(current))
            chunks.Add(current.Trim());

        return chunks;
    }

    private static string ExtractFormName(string fileName) =>
        fileName.Replace("-", " ").Replace("_", " ");
}