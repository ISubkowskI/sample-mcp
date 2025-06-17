using System.ComponentModel.DataAnnotations;

namespace Ae.Sample.Mcp.Services;

public interface IDtoValidator
{
    bool TryValidate(object obj, out ICollection<ValidationResult> validationResults);
}
