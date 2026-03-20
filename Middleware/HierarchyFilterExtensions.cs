using System.Linq;

public static class HierarchyFilterExtensions
{
    public static IQueryable<T> ApplyAccountHierarchyFilter<T>(
        this IQueryable<T> query,
        ICurrentUserService currentUser)
        where T : class, IAccountEntity
    {
        if (currentUser.IsSystemRole)
            return query;

        var accessibleAccountIds = currentUser.AccessibleAccountIds;
        if (accessibleAccountIds == null || accessibleAccountIds.Count == 0)
            return query.Where(_ => false);

        return query.Where(x => accessibleAccountIds.Contains(x.AccountId));
    }
}
