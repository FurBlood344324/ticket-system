namespace TicketSupport.Models;

public class TicketFilterViewModel
{
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
    public int? DepartmentId { get; set; }
    public List<int> TagIds { get; set; } = [];
    public int? AssignedToId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsUnassigned { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<SupportTicket> Tickets { get; set; } = [];
    public List<Department> AvailableDepartments { get; set; } = [];
    public List<TicketTag> AvailableTags { get; set; } = [];
    public List<AppUser> AvailableAgents { get; set; } = [];
    public List<AppUser> AvailableCustomers { get; set; } = [];
    public List<int> PageSizeOptions { get; set; } = [20, 50, 100];
}
