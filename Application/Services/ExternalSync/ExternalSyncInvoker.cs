using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncInvoker : IExternalSyncInvoker
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<string, InvocationMetadata> _invocationCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, Type> _typeCache = new(StringComparer.Ordinal);

    public ExternalSyncInvoker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(
        external_sync_config config,
        string entityId,
        string? payloadJson,
        CancellationToken ct = default)
    {
        var cacheKey = $"{config.ServiceInterface}|{config.ServiceMethod}";
        var metadata = _invocationCache.GetOrAdd(cacheKey, _ => BuildInvocationMetadata(config));

        var service = _serviceProvider.GetService(metadata.InterfaceType)
            ?? throw new InvalidOperationException($"Service not registered in DI: {config.ServiceInterface}");

        var payload = string.IsNullOrWhiteSpace(payloadJson) ? entityId : payloadJson;
        object? result;

        if (metadata.Signature == InvocationSignature.PayloadAndCancellationToken)
        {
            result = metadata.Method.Invoke(service, new object?[] { payload, ct });
        }
        else if (metadata.Signature == InvocationSignature.PayloadOnly)
        {
            result = metadata.Method.Invoke(service, new object?[] { payload });
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

    private static InvocationMetadata BuildInvocationMetadata(external_sync_config config)
    {
        var interfaceType = ResolveType(config.ServiceInterface)
            ?? throw new InvalidOperationException($"Service interface type not found: {config.ServiceInterface}");

        var method = interfaceType.GetMethod(config.ServiceMethod, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method {config.ServiceMethod} not found on {config.ServiceInterface}");

        var parameters = method.GetParameters();
        var signature = parameters.Length switch
        {
            1 when parameters[0].ParameterType == typeof(string) => InvocationSignature.PayloadOnly,
            2 when parameters[0].ParameterType == typeof(string)
                   && parameters[1].ParameterType == typeof(CancellationToken) => InvocationSignature.PayloadAndCancellationToken,
            _ => InvocationSignature.Unsupported
        };

        if (signature == InvocationSignature.Unsupported)
        {
            throw new InvalidOperationException(
                $"Unsupported method signature for {config.ServiceInterface}.{config.ServiceMethod}. " +
                "Expected (string) or (string, CancellationToken).");
        }

        return new InvocationMetadata(interfaceType, method, signature);
    }

    private static Type? ResolveType(string typeName)
    {
        if (_typeCache.TryGetValue(typeName, out var cached))
        {
            return cached;
        }

        var direct = Type.GetType(typeName);
        if (direct != null)
        {
            _typeCache[typeName] = direct;
            return direct;
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }

            var found = types.FirstOrDefault(t =>
                t.FullName == typeName || t.Name == typeName);
            if (found != null)
            {
                _typeCache[typeName] = found;
                return found;
            }
        }

        return null;
    }

    private sealed record InvocationMetadata(Type InterfaceType, MethodInfo Method, InvocationSignature Signature);

    private enum InvocationSignature
    {
        Unsupported = 0,
        PayloadOnly = 1,
        PayloadAndCancellationToken = 2
    }
}
