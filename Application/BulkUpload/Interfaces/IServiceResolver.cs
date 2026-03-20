public interface IServiceResolver
{
    object ResolveService(string interfaceName);
    System.Type ResolveDtoType(string dtoName);
}
