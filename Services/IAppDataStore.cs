using TicketSupport.Models;

namespace TicketSupport.Services;

public interface IAppDataStore
{
    List<AppUser> GetUsers();
    AppUser? FindUserByEmail(string email);
    AppUser AddCustomer(RegisterViewModel model);
    bool IsPasswordValid(AppUser user, string password);

    List<SupportTicket> GetTickets();
    TicketFilterViewModel GetFilteredTickets(TicketFilterViewModel filter, AppUser currentUser);
    SupportTicket? FindTicket(int id);
    SupportTicket? FindTicketDetails(int id);
    TicketAttachment? FindAttachment(int id);
    List<TicketTemplate> GetActiveTicketTemplates();
    SlaPolicy? GetSlaPolicy(TicketPriority priority);
    SupportTicket AddTicket(CreateTicketViewModel model, AppUser customer, IReadOnlyCollection<TicketAttachment> attachments);
    void AssignTicket(int ticketId, AppUser supportUser);
    void AddReply(int ticketId, TicketReplyViewModel model, AppUser author, IReadOnlyCollection<TicketAttachment> attachments);
    void AddTimeEntry(int ticketId, TicketTimeEntryViewModel model, AppUser user, string? auditAction = null);
    void UpdateTicket(int ticketId, TicketEditViewModel model, AppUser actor);
    void UpdateStatus(int ticketId, TicketStatus status, AppUser actor);

    DashboardViewModel GetDashboardData(AppUser currentUser);
}
