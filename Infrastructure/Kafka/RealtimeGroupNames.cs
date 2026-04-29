public static class RealtimeGroupNames
{
    public static string Org(int orgId) => $"org:{orgId}";
    public static string Topic(string topic) => $"topic:{NormalizeTopic(topic)}";
    public static string OrgTopic(int orgId, string topic) => $"{Org(orgId)}:{Topic(topic)}";

    public static string NormalizeTopic(string topic) =>
        string.IsNullOrWhiteSpace(topic) ? "unknown" : topic.Trim().ToLowerInvariant();
}
