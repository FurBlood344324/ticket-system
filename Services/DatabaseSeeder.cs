using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public static class DatabaseSeeder
{
    public static void Seed(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        SeedDepartments(dbContext);
        SeedUsers(dbContext, passwordHasher);
        SeedTickets(dbContext);
    }

    private static void SeedDepartments(ApplicationDbContext dbContext)
    {
        if (dbContext.Departments.Any())
        {
            return;
        }

        dbContext.Departments.AddRange(
            new Department { Name = "BT Destek", Description = "Bilgi teknolojileri destek departmanı" },
            new Department { Name = "İK", Description = "İnsan kaynakları departmanı" },
            new Department { Name = "Muhasebe", Description = "Muhasebe ve finans departmanı" }
        );

        dbContext.SaveChanges();
    }

    private static void SeedUsers(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        var adminUser = dbContext.Users.FirstOrDefault(user => user.Email == "admin@ticket.local");
        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                FullName = "Sistem Yöneticisi",
                Email = "admin@ticket.local",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.Admin
            };
            dbContext.Users.Add(adminUser);
        }

        var itDepartment = dbContext.Departments.First(department => department.Name == "BT Destek");

        var supportUser = dbContext.Users.FirstOrDefault(user => user.Email == "destek@ticket.local");
        if (supportUser is null)
        {
            supportUser = new AppUser
            {
                FullName = "Destek Personeli",
                Email = "destek@ticket.local",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.Support,
                DepartmentId = itDepartment.Id
            };
            dbContext.Users.Add(supportUser);
        }
        else if (supportUser.DepartmentId is null)
        {
            supportUser.DepartmentId = itDepartment.Id;
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
    }

    private static void SeedTickets(ApplicationDbContext dbContext)
    {
        if (dbContext.Tickets.Any())
        {
            return;
        }

        var customer = dbContext.Users.First(user => user.Email == "musteri@ticket.local");

        dbContext.Tickets.Add(new SupportTicket
        {
            Title = "Demo destek talebi",
            Description = "Bu kayıt uygulamayı ilk çalıştırdığınızda liste ve detay ekranını görmek için eklenmiştir.",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Other,
            CustomerId = customer.Id,
            CustomerName = customer.FullName
        });

        dbContext.SaveChanges();
    }
}
