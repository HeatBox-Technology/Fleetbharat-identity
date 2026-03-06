using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public class ValidationService : IValidationService
{
    public List<string> ValidateObject(object dto)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);

        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        return results
            .Select(r => r.ErrorMessage ?? "Validation failed")
            .ToList();
    }
}
