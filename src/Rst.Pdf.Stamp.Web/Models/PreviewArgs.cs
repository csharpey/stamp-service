using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Rst.Pdf.Stamp.Web.Models;

public class PreviewArgs : IValidatableObject
{
    [Required] public IFormFile File { get; set; }

    [Required] [FromForm(Name = "sig")] public IFormFileCollection Signatures { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Signatures.Any())
        {
            var message = $"{nameof(Signatures)} could not be empty";
            yield return new ValidationResult(message, new[] { nameof(Signatures) });
        }
    }
}