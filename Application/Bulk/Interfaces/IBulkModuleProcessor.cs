using System.Threading.Tasks;

public interface IBulkModuleProcessor
{
    string ModuleName { get; }

    Task ProcessAsync(string payloadJson);

}