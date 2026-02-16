using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace CopilotStudioTestRunner.Core.DocumentProcessing;

public class ChunkerOptions
{
    public int ChunkSize { get; set; } = 1000; // tokens/characters
    public int OverlapSize { get; set; } = 200;
}

public interface IDocumentChunker
{
    List<ChunkData> ChunkText(string text, ChunkerOptions? options = null);
}

public class ChunkData
{
    public string Text { get; set; } = string.Empty;
    public int Index { get; set; }
    public int TokenEstimate { get; set; }
}

public class DocumentChunker : IDocumentChunker
{
    private readonly ILogger _logger = Log.ForContext<DocumentChunker>();

    public List<ChunkData> ChunkText(string text, ChunkerOptions? options = null)
    {
        options ??= new ChunkerOptions();
        var chunks = new List<ChunkData>();

        // Split by paragraphs first
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        int chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > options.ChunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(new ChunkData
                    {
                        Text = currentChunk.ToString().Trim(),
                        Index = chunkIndex,
                        TokenEstimate = EstimateTokens(currentChunk.ToString())
                    });
                    chunkIndex++;

                    // Add overlap
                    var overlap = currentChunk.ToString()
                        .Substring(Math.Max(0, currentChunk.Length - options.OverlapSize));
                    currentChunk.Clear();
                    currentChunk.Append(overlap);
                }
            }

            currentChunk.AppendLine(paragraph);
        }

        // Add final chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(new ChunkData
            {
                Text = currentChunk.ToString().Trim(),
                Index = chunkIndex,
                TokenEstimate = EstimateTokens(currentChunk.ToString())
            });
        }

        _logger.Information("Created {ChunkCount} chunks from text", chunks.Count);
        return chunks;
    }

    private int EstimateTokens(string text)
    {
        // Simple token estimation: average 4 characters per token
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
