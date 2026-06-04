namespace TicketSupport.Models;

public class DashboardViewModel
{
    public string UserRole { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    // Stat cards
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int SolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public int SlaBreachCount { get; set; }
    public int TodayResolvedCount { get; set; }
    public int UnassignedCount { get; set; }
    public int TotalCount { get; set; }

    // Chart data
    public List<string> CategoryLabels { get; set; } = [];
    public List<int> CategoryCounts { get; set; } = [];

    public List<string> PriorityLabels { get; set; } = [];
    public List<int> PriorityCounts { get; set; } = [];

    public List<string> TrendLabels { get; set; } = [];  // Last 7 days dates
    public List<int> TrendCounts { get; set; } = [];      // Ticket counts per day

    // SLA breach tickets table
    public List<SlaBreachItem> SlaBreachTickets { get; set; } = [];

    // Recent updates
    public List<RecentUpdateItem> RecentUpdates { get; set; } = [];
}

public class SlaBreachItem
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RecentUpdateItem
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
