using TicketSupport.Data;
using TicketSupport.Models;

namespace TicketSupport.Services;

public static class DatabaseSeeder
{
    public static void Seed(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        SeedOrganizations(dbContext);
        SeedDepartments(dbContext);
        SeedUsers(dbContext, passwordHasher);
        SeedSlaPolicies(dbContext);
        SeedTicketTags(dbContext);
        SeedTickets(dbContext);
        SeedTicketReplies(dbContext);
        SeedTicketAuditEvents(dbContext);
        SeedKnowledgeCategories(dbContext);
        SeedKnowledgeArticles(dbContext);
        SeedRelatedArticles(dbContext);
        SeedTicketTemplates(dbContext);
        SeedWebhookSubscriptions(dbContext);
        SeedTicketTimeEntries(dbContext);
        SeedUserNotifications(dbContext);
        SeedRecurringTickets(dbContext);
        SeedApiKeys(dbContext, passwordHasher);
    }

    private static void SeedOrganizations(ApplicationDbContext dbContext)
    {
        if (dbContext.CustomerOrganizations.Any())
        {
            return;
        }

        dbContext.CustomerOrganizations.Add(new CustomerOrganization
        {
            CompanyName = "Demo Teknoloji A.Ş.",
            TaxNumber = "1234567890",
            Phone = "+90 212 555 0000",
            Address = "Levent Mah. Teknoloji Cad. No:42, Beşiktaş, İstanbul"
        });

        dbContext.SaveChanges();
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
        var demoOrg = dbContext.CustomerOrganizations.First(org => org.CompanyName == "Demo Teknoloji A.Ş.");

        var adminUser = dbContext.Users.FirstOrDefault(user => user.Email == "admin@ticket.local");
        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                FullName = "Sistem Yöneticisi",
                Email = "admin@ticket.local",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.Admin,
                IsActive = true,
                JobTitle = "BT Yöneticisi",
                PreferredLanguage = "tr",
                DarkMode = false,
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true,
                Signature = "İyi çalışmalar,\nSistem Yöneticisi"
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
                DepartmentId = itDepartment.Id,
                IsActive = true,
                JobTitle = "Destek Uzmanı",
                PreferredLanguage = "tr",
                DarkMode = true,
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true,
                Signature = "Saygılarımla,\nBT Destek Ekibi"
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
                Role = UserRole.Customer,
                OrganizationId = demoOrg.Id,
                IsActive = true,
                JobTitle = "Yazılım Geliştirici",
                Phone = "+90 555 123 4567",
                PreferredLanguage = "tr",
                DarkMode = false,
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = false
            };
            dbContext.Users.Add(customer);
        }
        else if (customer.OrganizationId is null)
        {
            customer.OrganizationId = demoOrg.Id;
        }

        dbContext.SaveChanges();
    }

    private static void SeedSlaPolicies(ApplicationDbContext dbContext)
    {
        if (dbContext.SlaPolicies.Any())
        {
            return;
        }

        dbContext.SlaPolicies.AddRange(
            new SlaPolicy { Priority = TicketPriority.Low, ResponseTimeMinutes = 480, ResolutionTimeMinutes = 2880, BusinessHoursOnly = true },
            new SlaPolicy { Priority = TicketPriority.Medium, ResponseTimeMinutes = 240, ResolutionTimeMinutes = 1440, BusinessHoursOnly = true },
            new SlaPolicy { Priority = TicketPriority.High, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 480, BusinessHoursOnly = false },
            new SlaPolicy { Priority = TicketPriority.Critical, ResponseTimeMinutes = 15, ResolutionTimeMinutes = 120, BusinessHoursOnly = false }
        );

        dbContext.SaveChanges();
    }

    private static void SeedTicketTags(ApplicationDbContext dbContext)
    {
        if (dbContext.TicketTags.Any())
        {
            return;
        }

        dbContext.TicketTags.AddRange(
            new TicketTag { Name = "acil", Color = "#dc3545" },
            new TicketTag { Name = "donanım", Color = "#0d6efd" },
            new TicketTag { Name = "yazılım", Color = "#198754" },
            new TicketTag { Name = "ağ", Color = "#6f42c1" },
            new TicketTag { Name = "eğitim", Color = "#fd7e14" },
            new TicketTag { Name = "beklemede", Color = "#6c757d" }
        );

        dbContext.SaveChanges();
    }

    private static void SeedTickets(ApplicationDbContext dbContext)
    {
        if (dbContext.Tickets.Any())
        {
            return;
        }

        var customer = dbContext.Users.First(user => user.Email == "musteri@ticket.local");
        var supportUser = dbContext.Users.First(user => user.Email == "destek@ticket.local");
        var itDepartment = dbContext.Departments.First(department => department.Name == "BT Destek");
        var tags = dbContext.TicketTags.ToList();

        dbContext.Tickets.AddRange(
            new SupportTicket
            {
                Title = "Demo destek talebi",
                Description = "Bu kayıt uygulamayı ilk çalıştırdığınızda liste ve detay ekranını görmek için eklenmiştir.",
                Priority = TicketPriority.Medium,
                Category = TicketCategory.Other,
                CustomerId = customer.Id,
                CustomerName = customer.FullName
            },
            new SupportTicket
            {
                Title = "Bilgisayar açılmıyor",
                Description = "Sabah geldiğimde bilgisayarım açılmıyor. Güç düğmesine basıyorum ama hiçbir tepki yok. Dün akşam normal kapatmıştım.",
                Status = TicketStatus.InProgress,
                Priority = TicketPriority.High,
                Category = TicketCategory.Hardware,
                CustomerId = customer.Id,
                CustomerName = customer.FullName,
                AssignedSupportId = supportUser.Id,
                AssignedSupportName = supportUser.FullName,
                DepartmentId = itDepartment.Id
            },
            new SupportTicket
            {
                Title = "E-posta hesabına erişemiyorum",
                Description = "Outlook sürekli şifre soruyor ve doğru şifreyi girmeme rağmen kabul etmiyor. Webmail üzerinden de giriş yapamıyorum.",
                Status = TicketStatus.WaitingCustomer,
                Priority = TicketPriority.Medium,
                Category = TicketCategory.Email,
                CustomerId = customer.Id,
                CustomerName = customer.FullName,
                AssignedSupportId = supportUser.Id,
                AssignedSupportName = supportUser.FullName,
                DepartmentId = itDepartment.Id,
                DueDate = DateTime.UtcNow.AddDays(2)
            }
        );

        dbContext.SaveChanges();

        // Attach tags to the second ticket
        var ikinciTicket = dbContext.Tickets.OrderBy(t => t.Id).Skip(1).First();
        var acilTag = tags.First(t => t.Name == "acil");
        var donanimTag = tags.First(t => t.Name == "donanım");

        if (!dbContext.TicketTagRelations.Any(r => r.TicketId == ikinciTicket.Id && r.TagId == acilTag.Id))
        {
            dbContext.TicketTagRelations.Add(new TicketTagRelation { TicketId = ikinciTicket.Id, TagId = acilTag.Id });
        }

        if (!dbContext.TicketTagRelations.Any(r => r.TicketId == ikinciTicket.Id && r.TagId == donanimTag.Id))
        {
            dbContext.TicketTagRelations.Add(new TicketTagRelation { TicketId = ikinciTicket.Id, TagId = donanimTag.Id });
        }

        dbContext.SaveChanges();
    }

    private static void SeedTicketReplies(ApplicationDbContext dbContext)
    {
        if (dbContext.TicketReplies.Any())
        {
            return;
        }

        var ticket = dbContext.Tickets.OrderBy(t => t.Id).Skip(1).First();
        var supportUser = dbContext.Users.First(user => user.Email == "destek@ticket.local");

        dbContext.TicketReplies.Add(new TicketReply
        {
            TicketId = ticket.Id,
            AuthorId = supportUser.Id,
            AuthorName = supportUser.FullName,
            AuthorRole = UserRole.Support,
            Message = "Merhaba, sorununuzu inceledik. Güç adaptörünü kontrol eder misiniz? Fişin takılı olduğundan ve adaptör ışığının yandığından emin olun."
        });

        dbContext.SaveChanges();
    }

    private static void SeedTicketAuditEvents(ApplicationDbContext dbContext)
    {
        if (dbContext.TicketAuditEvents.Any())
        {
            return;
        }

        var ticket = dbContext.Tickets.First();
        var supportUser = dbContext.Users.First(user => user.Email == "destek@ticket.local");

        dbContext.TicketAuditEvents.AddRange(
            new TicketAuditEvent
            {
                TicketId = ticket.Id,
                ActorId = supportUser.Id,
                ActorName = supportUser.FullName,
                Action = "ticket.created",
                NewValue = "Ticket oluşturuldu",
                IpAddress = "192.168.1.100"
            },
            new TicketAuditEvent
            {
                TicketId = ticket.Id,
                ActorId = supportUser.Id,
                ActorName = supportUser.FullName,
                Action = "ticket.priority_changed",
                OldValue = "Medium",
                NewValue = "High",
                IpAddress = "192.168.1.100"
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedKnowledgeCategories(ApplicationDbContext dbContext)
    {
        if (dbContext.KnowledgeCategories.Any())
        {
            return;
        }

        dbContext.KnowledgeCategories.AddRange(
            new KnowledgeCategory { Name = "Sık Sorulan Sorular", Description = "En çok karşılaşılan sorunlar ve çözümleri", SortOrder = 1 },
            new KnowledgeCategory { Name = "Kurulum Rehberleri", Description = "Yazılım ve donanım kurulum adımları", SortOrder = 2 },
            new KnowledgeCategory { Name = "Sorun Giderme", Description = "Yaygın hatalar ve çözüm yöntemleri", SortOrder = 3 },
            new KnowledgeCategory { Name = "Politika ve Prosedürler", Description = "Şirket BT politikaları ve prosedürleri", SortOrder = 4 }
        );

        dbContext.SaveChanges();
    }

    private static void SeedKnowledgeArticles(ApplicationDbContext dbContext)
    {
        if (dbContext.KnowledgeArticles.Any())
        {
            return;
        }

        var adminUser = dbContext.Users.First(user => user.Email == "admin@ticket.local");
        var sssCategory = dbContext.KnowledgeCategories.First(cat => cat.Name == "Sık Sorulan Sorular");
        var kurulumCategory = dbContext.KnowledgeCategories.First(cat => cat.Name == "Kurulum Rehberleri");

        dbContext.KnowledgeArticles.AddRange(
            new KnowledgeArticle
            {
                Title = "VPN Bağlantısı Nasıl Kurulur?",
                Slug = "vpn-baglantisi-nasil-kurulur",
                Content = """
                    # VPN Bağlantı Kurulumu

                    ## Gereksinimler
                    - Şirket VPN istemcisi (OpenVPN)
                    - Geçerli şirket hesabı

                    ## Adımlar
                    1. OpenVPN istemcisini [buradan](https://openvpn.net) indirin
                    2. Kurulumu tamamlayın
                    3. Şirket VPN profil dosyasını IT departmanından talep edin
                    4. Profili OpenVPN'e aktarın
                    5. Şirket kullanıcı adı ve şifrenizle bağlanın

                    ## Sorun Giderme
                    - Bağlantı hatası alıyorsanız internet bağlantınızı kontrol edin
                    - Sertifika hatası için IT ile iletişime geçin
                    """,
                KnowledgeCategoryId = sssCategory.Id,
                AuthorId = adminUser.Id,
                AuthorName = adminUser.FullName,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                ViewCount = 245,
                HelpfulCount = 42,
                NotHelpfulCount = 3,
                MetaKeywords = "vpn, bağlantı, openvpn, uzaktan erişim",
                MetaDescription = "Şirket VPN bağlantısının adım adım kurulum rehberi"
            },
            new KnowledgeArticle
            {
                Title = "E-posta İmza Ayarları",
                Slug = "eposta-imza-ayarlari",
                Content = """
                    # E-posta İmza Ayarları

                    ## Outlook için
                    1. Dosya > Seçenekler > Posta > İmzalar
                    2. Yeni imza oluşturun
                    3. Şirket şablonunu yapıştırın:
                    ```
                    Ad Soyad
                    Ünvan | Demo Teknoloji A.Ş.
                    Tel: +90 212 555 0000
                    E-posta: ad.soyad@demoteknoloji.com
                    ```
                    4. Varsayılan imza olarak ayarlayın

                    ## Webmail için
                    1. Ayarlar > İmza
                    2. İmza metnini girin
                    3. Kaydedin
                    """,
                KnowledgeCategoryId = kurulumCategory.Id,
                AuthorId = adminUser.Id,
                AuthorName = adminUser.FullName,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-15),
                ViewCount = 128,
                HelpfulCount = 31,
                NotHelpfulCount = 1,
                MetaKeywords = "e-posta, imza, outlook, webmail",
                MetaDescription = "Outlook ve Webmail için e-posta imza ayarları rehberi"
            },
            new KnowledgeArticle
            {
                Title = "Şifre Sıfırlama Politikası",
                Slug = "sifre-sifirlama-politikasi",
                Content = """
                    # Şifre Sıfırlama Politikası

                    ## Kurallar
                    - Şifreler en az 8 karakter olmalıdır
                    - En az 1 büyük harf, 1 küçük harf, 1 rakam içermelidir
                    - 90 günde bir şifre değişikliği zorunludur
                    - Son 5 şifre tekrar kullanılamaz

                    ## Şifre Sıfırlama Adımları
                    1. https://sifre.demoteknoloji.com adresine gidin
                    2. "Şifremi Unuttum" bağlantısına tıklayın
                    3. Şirket e-posta adresinizi girin
                    4. Gelen bağlantıya tıklayın
                    5. Yeni şifrenizi belirleyin

                    Hesabınız kilitlendiyse BT Destek ile iletişime geçin.
                    """,
                KnowledgeCategoryId = sssCategory.Id,
                AuthorId = adminUser.Id,
                AuthorName = adminUser.FullName,
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-60),
                ViewCount = 512,
                HelpfulCount = 89,
                NotHelpfulCount = 5,
                MetaKeywords = "şifre, sıfırlama, politika, güvenlik",
                MetaDescription = "Şirket şifre politikası ve şifre sıfırlama adımları"
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedRelatedArticles(ApplicationDbContext dbContext)
    {
        if (dbContext.RelatedArticles.Any())
        {
            return;
        }

        var vpnMakale = dbContext.KnowledgeArticles.First(a => a.Slug == "vpn-baglantisi-nasil-kurulur");
        var sifreMakale = dbContext.KnowledgeArticles.First(a => a.Slug == "sifre-sifirlama-politikasi");

        dbContext.RelatedArticles.Add(new RelatedArticle
        {
            ArticleId = vpnMakale.Id,
            RelatedArticleId = sifreMakale.Id
        });

        dbContext.SaveChanges();
    }

    private static void SeedTicketTemplates(ApplicationDbContext dbContext)
    {
        if (dbContext.TicketTemplates.Any())
        {
            return;
        }

        var supportUser = dbContext.Users.First(user => user.Email == "destek@ticket.local");
        var itDepartment = dbContext.Departments.First(department => department.Name == "BT Destek");

        dbContext.TicketTemplates.AddRange(
            new TicketTemplate
            {
                Title = "Yeni Donanım Talebi",
                Description = "Yeni bilgisayar, monitör veya çevre birimi talepleri için",
                Category = TicketCategory.Hardware,
                Priority = TicketPriority.Low,
                Content = """
                    ## Donanım Talebi

                    **Talep Edilen Donanım:**
                    - Ürün:
                    - Adet:
                    - Gerekçe:

                    **Teslimat Bilgileri:**
                    - Departman:
                    - Teslim Adresi:
                    """,
                DepartmentId = itDepartment.Id,
                CreatedById = supportUser.Id,
                CreatedByName = supportUser.FullName,
                UsageCount = 15
            },
            new TicketTemplate
            {
                Title = "Yazılım Kurulum Talebi",
                Description = "Yeni yazılım kurulumu veya lisans talepleri için",
                Category = TicketCategory.Software,
                Priority = TicketPriority.Medium,
                Content = """
                    ## Yazılım Kurulum Talebi

                    **Yazılım Bilgileri:**
                    - Yazılım Adı:
                    - Sürüm:
                    - Lisans Türü:

                    **Kullanıcı Bilgileri:**
                    - Ad Soyad:
                    - Departman:
                    - Gerekçe:
                    """,
                DepartmentId = itDepartment.Id,
                CreatedById = supportUser.Id,
                CreatedByName = supportUser.FullName,
                UsageCount = 8
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedWebhookSubscriptions(ApplicationDbContext dbContext)
    {
        if (dbContext.WebhookSubscriptions.Any())
        {
            return;
        }

        dbContext.WebhookSubscriptions.AddRange(
            new WebhookSubscription
            {
                Name = "Slack Bildirim Entegrasyonu",
                Url = "https://hooks.slack.com/services/demo/webhook-url",
                Secret = "whsec_demo_slack_secret_key_123",
                EventTypes = "ticket.created,ticket.updated,ticket.resolved",
                LastTriggeredAt = DateTime.UtcNow.AddHours(-2)
            },
            new WebhookSubscription
            {
                Name = "Harici Raporlama API",
                Url = "https://api.raporlama.demo/webhooks/ticket-events",
                Secret = "whsec_demo_reporting_secret_456",
                EventTypes = "ticket.created,ticket.closed,ticket.resolved",
                LastTriggeredAt = DateTime.UtcNow.AddHours(-5)
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedTicketTimeEntries(ApplicationDbContext dbContext)
    {
        if (dbContext.TicketTimeEntries.Any())
        {
            return;
        }

        var ticket = dbContext.Tickets.OrderBy(t => t.Id).Skip(1).First(); // "Bilgisayar açılmıyor"
        var supportUser = dbContext.Users.First(user => user.Email == "destek@ticket.local");

        dbContext.TicketTimeEntries.AddRange(
            new TicketTimeEntry
            {
                TicketId = ticket.Id,
                UserId = supportUser.Id,
                UserName = supportUser.FullName,
                Minutes = 30,
                Description = "Kullanıcı ile telefon görüşmesi yapıldı, sorun detayları alındı",
                ActivityType = TicketActivityType.Communication,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new TicketTimeEntry
            {
                TicketId = ticket.Id,
                UserId = supportUser.Id,
                UserName = supportUser.FullName,
                Minutes = 45,
                Description = "Güç adaptörü ve batarya test edildi",
                ActivityType = TicketActivityType.Analysis,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new TicketTimeEntry
            {
                TicketId = ticket.Id,
                UserId = supportUser.Id,
                UserName = supportUser.FullName,
                Minutes = 60,
                Description = "Arızalı güç adaptörü değiştirildi, sistem test edildi",
                ActivityType = TicketActivityType.Work,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedUserNotifications(ApplicationDbContext dbContext)
    {
        if (dbContext.UserNotifications.Any())
        {
            return;
        }

        var supportUser = dbContext.Users.First(user => user.Email == "destek@ticket.local");
        var customer = dbContext.Users.First(user => user.Email == "musteri@ticket.local");
        var ticket = dbContext.Tickets.OrderBy(t => t.Id).Skip(1).First();

        dbContext.UserNotifications.AddRange(
            new UserNotification
            {
                UserId = supportUser.Id,
                Title = "Yeni destek talebi atandı",
                Message = "\"Bilgisayar açılmıyor\" başlıklı talep size atandı.",
                Url = $"/Tickets/Details/{ticket.Id}",
                IsRead = true,
                NotificationType = NotificationType.TicketAssigned,
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new UserNotification
            {
                UserId = customer.Id,
                Title = "Talebiniz yanıtlandı",
                Message = "\"Bilgisayar açılmıyor\" başlıklı talebinize yanıt geldi.",
                Url = $"/Tickets/Details/{ticket.Id}",
                IsRead = false,
                NotificationType = NotificationType.TicketReplied,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new UserNotification
            {
                UserId = supportUser.Id,
                Title = "SLA uyarısı",
                Message = "Yüksek öncelikli bir talebin yanıt süresi yaklaşıyor.",
                Url = "/Tickets",
                IsRead = false,
                NotificationType = NotificationType.SlaBreach,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedRecurringTickets(ApplicationDbContext dbContext)
    {
        if (dbContext.RecurringTickets.Any())
        {
            return;
        }

        var adminUser = dbContext.Users.First(user => user.Email == "admin@ticket.local");
        var itDepartment = dbContext.Departments.First(department => department.Name == "BT Destek");

        dbContext.RecurringTickets.AddRange(
            new RecurringTicket
            {
                Title = "Aylık sunucu bakım kontrolü",
                Description = "Tüm sunucuların disk kullanımı, RAM ve CPU metriklerinin kontrol edilmesi. Varsa kritik güncellemelerin uygulanması.",
                Category = TicketCategory.Software,
                Priority = TicketPriority.Medium,
                DepartmentId = itDepartment.Id,
                CronExpression = "0 9 1 * *",
                CreatedById = adminUser.Id,
                NextRunAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1).AddHours(9)
            },
            new RecurringTicket
            {
                Title = "Haftalık yedekleme doğrulaması",
                Description = "Günlük yedekleme loglarının kontrolü, son 7 günün yedeklerinden rastgele seçilen 2 tanesinin geri dönüş testinin yapılması.",
                Category = TicketCategory.Software,
                Priority = TicketPriority.High,
                DepartmentId = itDepartment.Id,
                CronExpression = "0 10 * * 1",
                CreatedById = adminUser.Id,
                NextRunAt = DateTime.UtcNow.AddDays((8 - (int)DateTime.UtcNow.DayOfWeek) % 7).Date.AddHours(10)
            }
        );

        dbContext.SaveChanges();
    }

    private static void SeedApiKeys(ApplicationDbContext dbContext, PasswordHasher passwordHasher)
    {
        if (dbContext.ApiKeys.Any())
        {
            return;
        }

        var adminUser = dbContext.Users.First(user => user.Email == "admin@ticket.local");

        dbContext.ApiKeys.Add(new ApiKey
        {
            Key = passwordHasher.Hash("ticket_sk_demo_live_key_1234567890abcdef"),
            Name = "Raporlama API Anahtarı",
            UserId = adminUser.Id,
            Scopes = "read:tickets,read:reports",
            LastUsedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        });

        dbContext.SaveChanges();
    }
}
