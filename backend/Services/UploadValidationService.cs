using System.IO.Compression;

namespace HelpDesk.Api.Services;

public interface IUploadValidationService
{
    Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken);
}

public sealed class UploadValidationService(IConfiguration configuration) : IUploadValidationService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".webp"] = ["image/webp"],
            [".pdf"] = ["application/pdf"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".txt"] = ["text/plain"]
        };

    public long MaxFileSize =>
        Math.Clamp(configuration.GetValue<long?>("Uploads:MaxFileSizeMb") ?? 10, 1, 25) * 1024 * 1024;

    public async Task<string?> ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            return "Choose a non-empty file.";
        }

        if (file.Length > MaxFileSize)
        {
            return $"File size must be {MaxFileSize / 1024 / 1024} MB or less.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedTypes.TryGetValue(extension, out var contentTypes) ||
            !contentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return "Allowed files: PNG, JPG, WEBP, PDF, DOCX, XLSX, and TXT.";
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[Math.Min(12, (int)file.Length)];
        _ = await stream.ReadAsync(header, cancellationToken);
        stream.Position = 0;

        return extension.ToLowerInvariant() switch
        {
            ".png" when !StartsWith(header, 0x89, 0x50, 0x4E, 0x47) => "The file content is not a valid PNG.",
            ".jpg" or ".jpeg" when !StartsWith(header, 0xFF, 0xD8, 0xFF) => "The file content is not a valid JPEG.",
            ".webp" when header.Length < 12 ||
                !header.AsSpan(0, 4).SequenceEqual("RIFF"u8) ||
                !header.AsSpan(8, 4).SequenceEqual("WEBP"u8) => "The file content is not a valid WEBP image.",
            ".pdf" when !header.AsSpan().StartsWith("%PDF-"u8) => "The file content is not a valid PDF.",
            ".docx" when !IsOfficePackage(stream, "word/") => "The file content is not a valid DOCX document.",
            ".xlsx" when !IsOfficePackage(stream, "xl/") => "The file content is not a valid XLSX workbook.",
            ".txt" when header.Contains((byte)0) => "The text file contains binary content.",
            _ => null
        };
    }

    private static bool IsOfficePackage(Stream stream, string requiredPrefix)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return archive.Entries.Any(entry =>
                entry.FullName.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool StartsWith(byte[] value, params byte[] signature) =>
        value.Length >= signature.Length && value.AsSpan(0, signature.Length).SequenceEqual(signature);
}
