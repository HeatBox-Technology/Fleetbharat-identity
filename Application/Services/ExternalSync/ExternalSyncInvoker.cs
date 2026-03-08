using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncInvoker : IExternalSyncInvoker
{
    private readonly IServiceProvider _serviceProvider;

    public ExternalSyncInvoker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(external_sync_config config, external_sync_queue queueItem, CancellationToken ct = default)
    {
        var interfaceType = ResolveType(config.ServiceInterface)
            ?? throw new InvalidOperationException($"Service interface type not found: {config.ServiceInterface}");

        var service = _serviceProvider.GetService(interfaceType)
            ?? throw new InvalidOperationException($"Service not registered in DI: {config.ServiceInterface}");

        var method = interfaceType.GetMethod(config.ServiceMethod, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method {config.ServiceMethod} not found on {config.ServiceInterface}");

        var parameters = method.GetParameters();
        object? result;

        if (parameters.Length == 2 &&
            parameters[0].ParameterType == typeof(string) &&
            parameters[1].ParameterType == typeof(CancellationToken))
        {
            result = method.Invoke(service, new object?[] { queueItem.PayloadJson, ct });
        }
        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
        {
            result = method.Invoke(service, new object?[] { queueItem.PayloadJson });
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported method signature for {config.ServiceInterface}.{config.ServiceMethod}. " +
                "Expected (string) or (string, CancellationToken).");
        }

        if (result is Task task)
            await task;
    }

    private static Type? ResolveType(string typeName)
    {
        var direct = Type.GetType(typeName);
        if (direct != null) return direct;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var found = asm.GetTypes().FirstOrDefault(t =>
                t.FullName == typeName || t.Name == typeName);
            if (found != null) return found;
        }

        return null;
    }
}
