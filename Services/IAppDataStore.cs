using TicketSupport.Models;

namespace TicketSupport.Services;

public interface IAppDataStore
{
    List<AppUser> GetUsers();
    AppUser? FindUserByEmail(string email);
    AppUser AddCustomer(RegisterViewModel model);
    bool IsPasswordValid(AppUser user, string password);

    List<SupportTicket> GetTickets();
    SupportTicket? FindTicket(int id);
    SupportTicket AddTicket(CreateTicketViewModel model, AppUser customer);
    void AssignTicket(int ticketId, AppUser supportUser);
    void AddReply(int ticketId, TicketReplyViewModel model, AppUser author);
}
