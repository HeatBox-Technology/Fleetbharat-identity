using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class VehicleBulkCustomValidator : IBulkCustomValidator
{
    private static readonly Regex VinRegex = new(
        "^[A-HJ-NPR-Z0-9]{17}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string ModuleKey => "vehicles";

    public Task<List<string>> ValidateAsync(
        Dictionary<string, string> row,
        object dto,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        var vehicleNumber = GetTrimmedString(dto, "VehicleNumber");
        if (!string.IsNullOrWhiteSpace(vehicleNumber) && vehicleNumber.Length > 20)
            errors.Add("Vehicle number cannot exceed 20 characters.");

        var vinOrChassisNumber = GetTrimmedString(dto, "VinOrChassisNumber");
        if (!string.IsNullOrWhiteSpace(vinOrChassisNumber) && !VinRegex.IsMatch(vinOrChassisNumber))
        {
            errors.Add("VIN/Chassis number must be exactly 17 characters and can contain only A-H, J-N, P, R-Z, and digits.");
        }

        return Task.FromResult(errors);
    }

    private static string GetTrimmedString(object dto, string propertyName)
    {
        var property = dto.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        return property?.GetValue(dto)?.ToString()?.Trim() ?? string.Empty;
    }
}
