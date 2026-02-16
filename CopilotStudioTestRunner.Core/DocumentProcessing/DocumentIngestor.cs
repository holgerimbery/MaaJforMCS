using System.Security.Cryptography;
using System.Text;
using UglyToad.PdfPig;
using Serilog;

namespace CopilotStudioTestRunner.Core.DocumentProcessing;

/// <summary>
/// Handles document ingestion and text extraction
/// </summary>
public interface IDocumentIngestor
{
    Task<(string text, int pageCount)> ExtractTextAsync(Stream fileStream, string documentType);
}

public class DocumentIngestor : IDocumentIngestor
{
    private readonly ILogger _logger = Log.ForContext<DocumentIngestor>();

    public async Task<(string text, int pageCount)> ExtractTextAsync(Stream fileStream, string documentType)
    {
        try
        {
            return documentType.ToLower() switch
            {
                "pdf" => ExtractFromPdf(fileStream),
                "text" or "txt" => ExtractFromText(fileStream),
                _ => throw new InvalidOperationException($"Unsupported document type: {documentType}")
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error extracting text from document");
            throw;
        }
    }

    private (string, int) ExtractFromPdf(Stream fileStream)
    {
        var sb = new StringBuilder();
        int pageCount = 0;

        using (var document = PdfDocument.Open(fileStream))
        {
            pageCount = document.NumberOfPages;
            for (int i = 1; i <= pageCount; i++)
            {
                var page = document.GetPage(i);
                var text = page.Text;
                sb.AppendLine($"--- Page {i} ---");
                sb.AppendLine(text);
                sb.AppendLine();
            }
        }

        _logger.Information("Extracted text from PDF with {PageCount} pages", pageCount);
        return (sb.ToString(), pageCount);
    }

    private (string, int) ExtractFromText(Stream fileStream)
    {
        using (var reader = new StreamReader(fileStream, Encoding.UTF8))
        {
            var text = reader.ReadToEnd();
            _logger.Information("Extracted text from text file");
            return (text, 1);
        }
    }

    public static string CalculateHash(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}
