using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public static class DatabaseSeeder
{
    public static void Seed(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        var supportUser = dbContext.Users.FirstOrDefault(user => user.Email == "destek@ticket.local");
        if (supportUser is null)
        {
            supportUser = new AppUser
            {
                FullName = "Destek Personeli",
                Email = "destek@ticket.local",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.Support
            };
            dbContext.Users.Add(supportUser);
        }

        var customer = dbContext.Users.FirstOrDefault(user => user.Email == "musteri@ticket.local");
        if (customer is null)
        {
            customer = new AppUser
            {
                FullName = "Demo Müşteri",
                Email = "musteri@ticket.local",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.Customer
            };
            dbContext.Users.Add(customer);
        }

        dbContext.SaveChanges();

        if (dbContext.Tickets.Any())
        {
            return;
        }

        dbContext.Tickets.Add(new SupportTicket
        {
            Title = "Demo destek talebi",
            Description = "Bu kayıt uygulamayı ilk çalıştırdığınızda liste ve detay ekranını görmek için eklenmiştir.",
            CustomerId = customer.Id,
            CustomerName = customer.FullName
        });

        dbContext.SaveChanges();
    }
}
