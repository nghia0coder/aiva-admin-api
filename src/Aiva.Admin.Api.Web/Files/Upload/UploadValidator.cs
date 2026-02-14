namespace Aiva.Admin.Api.Web.Files.Upload;

using FluentValidation;
using Core.FileAggregate;
using System.Linq;

public class UploadFilesValidator : Validator<UploadFilesRequest>
{
  // Max total batch size: 100 MB
  private const long MaxTotalBatchSizeBytes = 100 * 1024 * 1024;
  // Max individual file size: 50 MB (keep reasonable individual limit)
  private const long MaxIndividualFileSizeBytes = 50 * 1024 * 1024;

  public UploadFilesValidator()
  {
    RuleFor(x => x.StorageId)
        .GreaterThan(0)
        .WithMessage("StorageId must be greater than 0");

    RuleFor(x => x.FolderId)
        .GreaterThan(0)
        .WithMessage("FolderId must be greater than 0");

    RuleFor(x => x.Files)
        .NotNull()
        .WithMessage("Files collection is required")
        .Must(files => files != null && files.Count > 0)
        .WithMessage("At least one file must be provided")
        .Must(files => files == null || files.Count <= 100) // Increased limit to 100
        .WithMessage("Maximum 100 files can be uploaded at once")
        .Must(files => files == null || files.Sum(f => f?.Length ?? 0) <= MaxTotalBatchSizeBytes)
        .WithMessage("Total size of all files cannot exceed 100 MB");

    RuleForEach(x => x.Files!)
        .Must(file => file != null)
        .WithMessage("File cannot be null")
        .Must(file => file == null || !string.IsNullOrWhiteSpace(file.FileName))
        .WithMessage("File name is required")
        .Must(file => file == null || file.Length > 0)
        .WithMessage("File cannot be empty")
        .Must(file => file == null || file.Length <= MaxIndividualFileSizeBytes)
        .WithMessage("Individual file size cannot exceed 50 MB")
        .Must(file => file == null || AllowedFileExtensions.IsAllowed(Path.GetExtension(file.FileName)))
        .WithMessage($"File extension is not allowed. Allowed: {string.Join(", ", AllowedFileExtensions.Extensions)}");
  }
}
