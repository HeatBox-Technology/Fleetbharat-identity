using System;
using System.Linq;

public class ServiceResolver : IServiceResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object ResolveService(string interfaceName)
    {
        var interfaceType = ResolveType(interfaceName);
        var service = _serviceProvider.GetService(interfaceType);

        if (service == null)
            throw new InvalidOperationException($"Service not registered for interface '{interfaceName}'.");

        return service;
    }

    public Type ResolveDtoType(string dtoName)
    {
        return ResolveType(dtoName);
    }

    private static Type ResolveType(string name)
    {
        var type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t =>
                string.Equals(t.FullName, name, StringComparison.Ordinal) ||
                string.Equals(t.Name, name, StringComparison.Ordinal));

        if (type == null)
            throw new InvalidOperationException($"Type '{name}' not found.");

        return type;
    }
}
