namespace TicketSupport.Models;

public class SlaPolicy
{
    public int Id { get; set; }
    public TicketPriority Priority { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public bool BusinessHoursOnly { get; set; }
}
