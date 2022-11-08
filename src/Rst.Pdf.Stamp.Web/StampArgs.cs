using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Rst.Pdf.Stamp.Web;

public class StampArgs : IValidatableObject
{
    public IFormFile Archive { get; set; }

    [FromForm(Name = "sig")]
    public IFormFileCollection Signatures { get; set; }

    [Required]
    [FromForm(Name = "file")]
    public IFormFileCollection Files { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Archive is not null && Signatures is not null)
        {
            var message = $"{nameof(Archive)} and {nameof(Signatures)} cannot be specified at the same time";
            yield return new ValidationResult(message, new[] { nameof(Archive), nameof(Signatures) });
        }

        foreach (var file in Files)
        {
            string message;
            try
            {
                new PdfReader(file.OpenReadStream());
                continue;
            }
            catch (InvalidPdfException e)
            {
                message = e.Message;
            }
            yield return new ValidationResult(message, new[] { nameof(Files) });
        }
    }
}

public class PreviewArgs : IValidatableObject
{
    [Required] public IFormFile File { get; set; }

    [Required][FromForm(Name = "sig")] public IFormFileCollection Signatures { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Signatures.Any())
        {
            var message = $"{nameof(Signatures)} could not be empty";
            yield return new ValidationResult(message, new[] { nameof(Signatures) });
        }
    }
}