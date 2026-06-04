using Microsoft.AspNetCore.Http;
using TicketSupport.Models;

namespace TicketSupport.Services;

public class FileAttachmentService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int MaxFileCount = 5;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".docx",
        ".xlsx",
        ".txt",
        ".zip",
        ".pptx"
    };

    private readonly string attachmentRoot;

    public FileAttachmentService(IWebHostEnvironment environment)
    {
        attachmentRoot = Path.Combine(environment.ContentRootPath, "App_Data", "attachments");
        Directory.CreateDirectory(attachmentRoot);
    }

    public async Task<List<TicketAttachment>> UploadAsync(
        IFormFileCollection files,
        AppUser uploadedBy,
        int ticketId = 0,
        int? replyId = null,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
        {
            return [];
        }

        if (files.Count > MaxFileCount)
        {
            throw new InvalidOperationException($"En fazla {MaxFileCount} dosya yükleyebilirsiniz.");
        }

        var attachments = new List<TicketAttachment>();

        try
        {
            foreach (var file in files)
            {
                ValidateFile(file);

                var extension = Path.GetExtension(file.FileName);
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var storagePath = Path.Combine(attachmentRoot, storedName);

                await using var stream = new FileStream(storagePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await file.CopyToAsync(stream, cancellationToken);

                attachments.Add(new TicketAttachment
                {
                    TicketId = ticketId,
                    ReplyId = replyId,
                    FileName = storedName,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    FileSize = file.Length,
                    StoragePath = storagePath,
                    UploadedById = uploadedBy.Id,
                    UploadedByName = uploadedBy.FullName
                });
            }
        }
        catch
        {
            foreach (var attachment in attachments)
            {
                await DeleteAsync(attachment);
            }

            throw;
        }

        return attachments;
    }

    public Task DeleteAsync(TicketAttachment attachment)
    {
        if (!string.IsNullOrWhiteSpace(attachment.StoragePath) && File.Exists(attachment.StoragePath))
        {
            File.Delete(attachment.StoragePath);
        }

        return Task.CompletedTask;
    }

    public void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Boş dosya yüklenemez.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"\"{Path.GetFileName(file.FileName)}\" dosyası 10 MB sınırını aşıyor.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"\"{Path.GetFileName(file.FileName)}\" için dosya uzantısına izin verilmiyor.");
        }
    }
}
