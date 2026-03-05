using System.Linq;

public static class AccountHierarchyFilter
{
    public static IQueryable<T> ApplyAccountScope<T>(
        this IQueryable<T> query,
        ICurrentUserService user)
        where T : class, IAccountEntity
    {
        return query.ApplyAccountHierarchyFilter(user);
    }
}
