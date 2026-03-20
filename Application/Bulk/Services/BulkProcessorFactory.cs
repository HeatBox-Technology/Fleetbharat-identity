using System.Collections.Generic;
using System.Linq;

public class BulkProcessorFactory
{
    private readonly IEnumerable<IBulkModuleProcessor> _processors;

    public BulkProcessorFactory(IEnumerable<IBulkModuleProcessor> processors)
    {
        _processors = processors;
    }

    public IBulkModuleProcessor Get(string module)
    {
        return _processors.First(x => x.ModuleName == module);
    }
}