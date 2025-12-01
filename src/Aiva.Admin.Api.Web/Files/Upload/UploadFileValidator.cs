using Aiva.Admin.Api.Core.FileAggregate;
using FluentValidation;

namespace Aiva.Admin.Api.Web.Files.Upload;

public class UploadFileValidator : Validator<UploadFileRequest>
{
  // Max file size: 50 MB
  private const long MaxFileSizeBytes = 50 * 1024 * 1024;

  private static readonly char[] InvalidBlobChars = { '\\', '/', '#', '?', '%', '&', '+', '"', '<', '>', '|', '*', ':' };

  public UploadFileValidator()
  {
    RuleFor(x => x.StorageId)
        .GreaterThan(0)
        .WithMessage("StorageId must be a positive number.");

    RuleFor(x => x.FolderId)
        .GreaterThan(0)
        .WithMessage("FolderId must be a positive number.");

    RuleFor(x => x.File)
        .NotNull()
        .WithMessage("File is required.")
        .Must(file => file is null || file.Length > 0)
        .WithMessage("File cannot be empty.")
        .Must(file => file is null || file.Length <= MaxFileSizeBytes)
        .WithMessage($"File size cannot exceed {MaxFileSizeBytes / (1024 * 1024)} MB.")
        .Must(HasAllowedExtension)
        .WithMessage($"File extension is not allowed. Allowed extensions: {string.Join(", ", AllowedFileExtensions.Extensions)}")
        .Must(file => file is null || !file.FileName.Any(c => InvalidBlobChars.Contains(c)))
        .WithMessage("File name contains invalid characters.");
  }

  private static bool HasAllowedExtension(IFormFile? file)
  {
    if (file is null) return true;

    var extension = Path.GetExtension(file.FileName);
    return AllowedFileExtensions.IsAllowed(extension);
  }
}
