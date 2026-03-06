using System.Collections.Generic;

public interface IValidationService
{
    List<string> ValidateObject(object dto);
}
