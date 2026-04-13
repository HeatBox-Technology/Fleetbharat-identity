using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class UniqueFieldValidator : IUniqueFieldValidator
{
    private readonly Dictionary<string, IBulkUniqueRule> _rules;

    public UniqueFieldValidator(IEnumerable<IBulkUniqueRule> rules)
    {
        _rules = rules.ToDictionary(x => x.ModuleKey, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string moduleKey,
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleKey))
            return (false, "Module key is required for uniqueness validation.");

        if (!_rules.TryGetValue(moduleKey, out var rule))
            return (false, $"No uniqueness validator is registered for module '{moduleKey}'.");

        return await rule.ValidateAsync(propertyName, value, scopeValues, cancellationToken);
    }
}
