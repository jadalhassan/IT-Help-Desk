namespace HelpDesk.Api.Services;

public static class TicketWorkflow
{
    public static readonly string[] Categories = ["Bug", "Feature Request", "Support", "Billing", "General"];
    public static readonly string[] Priorities = ["Low", "Medium", "High", "Urgent"];
    public static readonly string[] Statuses = ["Open", "Assigned", "In Progress", "Waiting for User", "Resolved", "Closed"];

    private static readonly IReadOnlyDictionary<string, string[]> Transitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Open"] = ["Assigned", "In Progress", "Closed"],
            ["Assigned"] = ["In Progress", "Waiting for User", "Resolved", "Closed"],
            ["In Progress"] = ["Waiting for User", "Resolved", "Closed"],
            ["Waiting for User"] = ["In Progress", "Resolved", "Closed"],
            ["Resolved"] = ["In Progress", "Closed"],
            ["Closed"] = ["Open"]
        };

    public static bool CanTransition(string currentStatus, string nextStatus) =>
        string.Equals(currentStatus, nextStatus, StringComparison.OrdinalIgnoreCase) ||
        Transitions.TryGetValue(currentStatus, out var allowed) &&
        allowed.Contains(nextStatus, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> AllowedTransitions(string currentStatus) =>
        Transitions.TryGetValue(currentStatus, out var allowed) ? allowed : [];
}
