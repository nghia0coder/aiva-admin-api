using System.ComponentModel.DataAnnotations;

namespace Aiva.Admin.Api.Web.Contributors.Create;

public class CreateContributorRequest
{
    public const string Route = "/Contributors";

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
}


