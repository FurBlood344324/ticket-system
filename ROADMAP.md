# 🎫 Giga IT Ticket Sistemi - Geliştirme Roadmap'i

## Mevcut Durum

ASP.NET Core MVC (.NET 10), EF Core + PostgreSQL, Cookie Auth (Customer/Support), temel ticket CRUD çalışıyor.

## Mimari Karar

**Phase 1 tüm data model'i dondurur.** Sonraki phase'lerde sıfır migration. Paralel çalışmanın tek yolu bu.

---

## PHASE 1 — Foundation (SEQUENTIAL → Codex)

6 issue, sırayla çalıştırılacak.

---

### Issue 1.1 — BCrypt + Güvenlik Altyapısı
**PC:** Codex | **Süre:** ~30 dk

```
ISSUE 1.1: BCrypt Şifreleme ve Güvenlik Altyapısı

Mevcut SHA256'yı BCrypt ile değiştir. Güvenlik altyapısını kur.

YAPILACAKLAR:
1. TicketSupport.csproj'a paket ekle: BCrypt.Net-Next
2. Services/PasswordHasher.cs'i komple yeniden yaz:
   - Hash(string password) → BCrypt.Net.BCrypt.HashPassword(password + pepper, workFactor: 12)
   - Verify(string password, string hash) → BCrypt.Net.BCrypt.Verify(password + pepper, hash)
   - Pepper'ı appsettings.json["Security:PasswordPepper"]'dan oku
3. appsettings.json'a ekle: "Security": { "PasswordPepper": "<random-guid>" }
4. Models/AppUser.cs PasswordHash kolon boyutunu 64'ten 120'ye çıkar (BCrypt 60 char)
5. Data/ApplicationDbContext.cs OnModelCreating'de PasswordHash max 120 yap
6. Services/DatabaseSeeder.cs: Hash() metodunu güncelle
7. Services/PostgresAppDataStore.cs IsPasswordValid içine migration logic ekle:
   - Eski 64-char hex hash'leri SHA256 ile doğrula → geçerliyse BCrypt'e çevirip kaydet
   - Yeni $2b$ hash'leri BCrypt.Verify ile kontrol et
8. Migration: dotnet ef migrations add BcryptUpgrade

KABUL:
- Hash'ler $2b$ ile başlıyor, 60 karakter
- Demo kullanıcılar giriş yapabiliyor
- Eski SHA256 hash'ler login'de otomatik migrate oluyor
```

---

### Issue 1.2 — Admin Rolü + Departman Modeli
**PC:** Codex | **Süre:** ~20 dk

```
ISSUE 1.2: Admin Rolü, Departman ve Organizasyon Yapısı

Admin rolü ekle, departman/organizasyon modellerini kur.

YAPILACAKLAR:
1. Models/UserRole.cs: Admin = 3 ekle
2. Models/Department.cs (yeni):
   - Id, Name, Description, CreatedAt, IsActive
3. Models/AppUser.cs'ye DepartmentId (int?, nullable) ekle
4. Models/SupportTicket.cs'ye DepartmentId (int?, nullable) ekle
5. Data/ApplicationDbContext.cs: Department DbSet + config, AppUser.DepartmentId, SupportTicket.DepartmentId
6. Services/DatabaseSeeder.cs:
   - Admin kullanıcısı: admin@ticket.local / 123456 / "Sistem Yöneticisi" / Admin
   - 3 departman: "BT Destek", "İK", "Muhasebe"
   - Support kullanıcısını BT Destek departmanına ata
7. Views/Shared/_Layout.cshtml: Admin nav linki
8. Migration: dotnet ef migrations add AdminAndDepartments

KABUL:
- Admin rolü var, admin@ticket.local çalışıyor
- Departman tablosu var, 3 seed departman
- Support kullanıcısı BT Destek'e bağlı
```

---

### Issue 1.3 — Priority, Category, Tag, SLA Modelleri
**PC:** Codex | **Süre:** ~40 dk

```
ISSUE 1.3: Öncelik, Kategori, Etiket ve SLA Veri Modelleri

Ticket'ı zenginleştirecek tüm enum ve modelleri ekle.

YAPILACAKLAR:
1. Models/TicketPriority.cs (yeni): Low=1, Medium=2, High=3, Critical=4
2. Models/TicketCategory.cs (yeni): Hardware=1, Software=2, Network=3, Account=4, Email=5, Other=6
3. Models/TicketStatus.cs: InProgress=4, WaitingCustomer=5, Cancelled=6 ekle
4. Models/TicketTag.cs (yeni):
   - Id, Name (unique), Color, CreatedAt
5. Models/TicketTagRelation.cs (yeni):
   - TicketId, TagId (many-to-many join)
6. Models/SlaPolicy.cs (yeni):
   - Id, Priority, ResponseTimeMinutes, ResolutionTimeMinutes, BusinessHoursOnly (bool)
7. Models/SupportTicket.cs güncelle:
   - Priority (TicketPriority, default Medium)
   - Category (TicketCategory, default Other)
   - Tags (many-to-many TicketTagRelation)
   - LastUpdatedAt (DateTime, auto-set)
   - DueDate (DateTime?, nullable)
   - FirstResponseAt (DateTime?, nullable)
   - ResolvedAt (DateTime?, nullable)
   - IsOverdue (bool, computed)
   - EscalationLevel (int, default 0)
8. Models/CreateTicketViewModel.cs: Priority, Category dropdown alanları ekle
9. Models/TicketListViewModel.cs: PriorityFilter, CategoryFilter, TagFilter ekle
10. Data/ApplicationDbContext.cs: tüm yeni entity'leri yapılandır
11. Migration: dotnet ef migrations add PriorityCategorySlaTags

KABUL:
- Tüm enum'lar ve modeller compile ediyor
- SupportTicket 10+ yeni kolona sahip
- Many-to-many Ticket-Tag ilişkisi kurulu
- Migration hatasız
```

---

### Issue 1.4 — Gelişmiş AppUser + Müşteri Organizasyonu
**PC:** Codex | **Süre:** ~30 dk

```
ISSUE 1.4: Gelişmiş Kullanıcı ve Müşteri Organizasyon Modeli

Kullanıcı modelini zenginleştir, şirket/ekip yapısını kur.

YAPILACAKLAR:
1. Models/CustomerOrganization.cs (yeni):
   - Id, CompanyName, TaxNumber, Phone, Address, CreatedAt, IsActive
2. Models/AppUser.cs'ye ekle:
   - CreatedAt (DateTime, default UtcNow)
   - LastLoginAt (DateTime?)
   - IsActive (bool, default true)
   - ProfileImageUrl (string?, max 500)
   - Phone (string?, max 20)
   - JobTitle (string?, max 100)
   - OrganizationId (int?, FK CustomerOrganization)
   - EmailNotificationsEnabled (bool, default true)
   - PushNotificationsEnabled (bool, default true)
   - TwoFactorEnabled (bool, default false)
   - TwoFactorSecretKey (string?, max 100)
   - PreferredLanguage (string, "tr", max 10)
   - DarkMode (bool, default false)
   - Signature (string?, max 500)
3. Models/NotificationPreference.cs (yeni):
   - Id, UserId, EventType (string), EmailEnabled, PushEnabled
4. Data/ApplicationDbContext.cs: tüm yeni config'leri ekle
5. Services/DatabaseSeeder.cs: demo şirket + güncellenmiş kullanıcılar
6. Migration: dotnet ef migrations add UserOrganizations

KABUL:
- AppUser 12+ yeni alana sahip
- CustomerOrganization tablosu var
- Demo müşteri bir organizasyona bağlı
```

---

### Issue 1.5 — TicketReply, Dahili Not, CannedResponse, Satisfaction Modelleri
**PC:** Codex | **Süre:** ~30 dk

```
ISSUE 1.5: Cevaplar, Dahili Notlar, Hazır Yanıtlar, Memnuniyet Anketi

Ticket iletişim ve geri bildirim modellerini tamamla.

YAPILACAKLAR:
1. Models/TicketReply.cs'ye ekle:
   - bool IsInternal (default false)
   - int? TemplateId (nullable)
   - TimeSpentMinutes (int?, nullable)
   - bool IsAiSuggested (default false)
2. Models/CannedResponse.cs (yeni):
   - Id, Title, Category, Content, CreatedById, CreatedByName, IsShared, UsageCount, CreatedAt
3. Models/CustomerSatisfaction.cs (yeni):
   - Id, TicketId, Rating (1-5), Comment, CreatedAt, IsVisibleToCustomer
4. Models/TicketReplyViewModel.cs: IsInternal, TimeSpentMinutes ekle
5. Models/TicketAttachment.cs (yeni - zaten vardı, kontrol et):
   - Id, TicketId, ReplyId (nullable), FileName, OriginalFileName, ContentType, FileSize, StoragePath, UploadedById, UploadedByName, CreatedAt
6. Data/ApplicationDbContext.cs: tüm yeni DbSet ve config'ler
7. Migration: dotnet ef migrations add ReplyEnhancements

KABUL:
- Dahili not özelliği modelde var
- Hazır yanıt (canned response) modeli var
- Müşteri memnuniyeti modeli var
- Attachment modeli güncel
```

---

### Issue 1.6 — Audit, Knowledge, Webhook, Template Modelleri
**PC:** Codex | **Süre:** ~40 dk

```
ISSUE 1.6: Audit Log, Bilgi Bankası, Webhook, Ticket Template Modelleri

Kalan TÜM veri modellerini ekle. Bu son migration.

YAPILACAKLAR:
1. Models/TicketAuditEvent.cs (yeni):
   - Id, TicketId, ActorId, ActorName, Action, OldValue, NewValue, CreatedAt, IpAddress
2. Models/KnowledgeArticle.cs (yeni):
   - Id, Title, Slug, Content (markdown), Category, AuthorId, AuthorName
   - IsPublished, CreatedAt, UpdatedAt, PublishedAt, ViewCount, HelpfulCount, NotHelpfulCount
   - MetaKeywords, MetaDescription
3. Models/KnowledgeCategory.cs (yeni):
   - Id, Name, Description, SortOrder, IsActive
4. Models/RelatedArticle.cs (yeni):
   - ArticleId, RelatedArticleId (self-referencing FK)
5. Models/TicketTemplate.cs (yeni):
   - Id, Title, Description, Category, Priority, Content (pre-filled ticket)
   - DepartmentId, CreatedById, CreatedByName, IsActive, UsageCount, CreatedAt
6. Models/WebhookSubscription.cs (yeni):
   - Id, Name, Url, Secret, EventTypes (string, comma-separated)
   - IsActive, CreatedAt, LastTriggeredAt, FailedCount
7. Models/TicketTimeEntry.cs (yeni):
   - Id, TicketId, UserId, UserName, Minutes, Description, ActivityType, CreatedAt
8. Models/UserNotification.cs (yeni):
   - Id, UserId, Title, Message, Url, IsRead, CreatedAt, NotificationType
9. Models/RecurringTicket.cs (yeni):
   - Id, Title, Description, Category, Priority, DepartmentId
   - CronExpression, IsActive, LastCreatedAt, NextRunAt, CreatedById
10. Models/ApiKey.cs (yeni):
    - Id, Key (hashed), Name, UserId, Scopes, IsActive, CreatedAt, LastUsedAt, ExpiresAt
11. Data/ApplicationDbContext.cs: TÜM entity config'leri EKSİKSİZ yap
    - Index'ler (foreign key'ler, sık sorgulanan alanlar)
    - Cascade delete kuralları
    - Max length'ler
    - Default değerler
12. SON MIGRASYON: dotnet ef migrations add CompleteDataModel
13. DatabaseSeeder.cs'i TÜM yeni entity'ler için demo veri ile güncelle

KABUL:
- 10 yeni entity, TÜM ilişkileriyle birlikte
- Migration hatasız, tüm tablolar oluşuyor
- Seed data tüm tabloları kapsıyor
- dotnet build 0 warning
- BUNDAN SONRA MIGRATION YOK
```

### ✅ PHASE 1 CHECKPOINT

Tüm data model dondu. İki PC paralel çalışmaya hazır.

---

## PHASE 2 — Core Features (PARALEL: Codex + OpenCode)

Phase 2'de 10 issue var. 5 Codex, 5 OpenCode. AYNI ANDA başlatılabilir.

---

### Issue 2.1 — Gelişmiş Arama, Filtreleme, Sayfalama, Sıralama
**PC:** Codex | **Bağımlılık:** Phase 1

```
ISSUE 2.1: Gelişmiş Arama, Çoklu Filtreleme, Sayfalama ve Sıralama

Ticket listesini enterprise seviyeye taşı.

YAPILACAKLAR:
1. Models/TicketFilterViewModel.cs (yeni):
   - Search (title + description full-text)
   - Status, Priority, Category, DepartmentId, TagIds
   - AssignedToId, CustomerId
   - FromDate, ToDate, DueDateFrom, DueDateTo
   - IsOverdue, IsUnassigned
   - SortBy (newest, oldest, priority_desc, priority_asc, due_date, last_updated)
   - Page, PageSize (20/50/100), TotalCount, TotalPages
   - List<SupportTicket> Tickets
2. Services/IAppDataStore.cs: TicketFilterViewModel GetFilteredTickets(...) ekle
3. Services/PostgresAppDataStore.cs: LINQ ile kompleks filtreleme
   - Full-text search: EF.Functions.ILike veya Contains
   - Tüm filtre kombinasyonları
   - Sayfalama
   - Tag filtresi (many-to-many)
4. Services/TicketQueryService.cs (yeni, opsiyonel): filtreleme logic'ini buraya çıkar
5. Controllers/TicketsController.cs: Index action'ı güncelle
6. Views/Tickets/Index.cshtml: komple yeniden yaz
   - Arama barı (sağ üst)
   - Accordion filtre paneli (sol veya üst)
   - Aktif filtre badge'leri (x ile kaldırma)
   - Tablo kolonları: checkbox, ID, Başlık, Öncelik(badge), Kategori, Durum, Departman, Müşteri, Atanan, SLA, Oluşturma
   - Sayfalama (1 2 3 ... 10)
   - Sayfa başına kayıt seçici (20/50/100)
   - Boş state: ikon + mesaj
7. Program.cs: yeni servis DI kaydı (gerekirse)
   // Issue 2.1

KABUL:
- 10+ filtre kombinasyonu çalışıyor
- Full-text arama Title+Description'da
- Sayfalama URL'de korunuyor
- Filtreleri temizle butonu
- Sayfa başına kayıt seçimi
```

---

### Issue 2.2 — Dashboard + SLA Paneli
**PC:** OpenCode | **Bağımlılık:** Phase 1

```
ISSUE 2.2: Canlı Dashboard ve SLA Takip Paneli

Her rol için özelleşmiş dashboard, SLA metrikleri, grafikler.

YAPILACAKLAR:
1. Controllers/DashboardController.cs (yeni):
   - [Authorize]
   - Index: role göre veri hazırla
   - Admin: tüm sistem metrikleri
   - Support: kendi departure ve atanmış ticket'ları
   - Customer: kendi ticket'ları
   - Veri: 
     - Açık/Devam Eden/Çözülen ticket sayıları
     - SLA ihlali olan ticket'lar
     - Bugün çözülenler
     - Atanmamış ticket'lar
     - Kategori dağılımı (chart data)
     - Öncelik dağılımı
     - Son 7 gün ticket trendi
2. Views/Dashboard/Index.cshtml (yeni):
   - Üst sıra: 4-6 istatistik kartı (renkli, ikonlu, tıklanabilir filtre linkleri)
   - Orta sıra: Chart.js bar chart (kategori dağılımı), donut chart (öncelik dağılımı)
   - Alt sıra: line chart (son 7 gün ticket trendi)
   - SLA ihlali olan ticket'lar tablosu (kırmızı vurgulu)
   - "Son Güncellemeler" canlı liste
3. wwwroot/js/dashboard.js (yeni): Chart.js ile grafikler
4. Chart.js CDN ekle (_Layout.cshtml veya importmap)
5. Controllers/AccountController.cs: Login sonrası Dashboard'a redirect
6. Views/Shared/_Layout.cshtml: Dashboard nav linki
7. Program.cs: // Issue 2.2

KABUL:
- 3 Chart.js grafiği
- Role'e özel istatistikler
- SLA ihlali vurgulaması
- Login → Dashboard
- Mobil responsive
```

---

### Issue 2.3 — Ticket Detay Sayfası + Dosya Eklentisi + Zaman Takibi
**PC:** Codex | **Bağımlılık:** Phase 1

```
ISSUE 2.3: Gelişmiş Ticket Detay, Dosya Eklentisi ve Zaman Takibi

Ticket detay sayfasını enterprise seviyeye taşı.

YAPILACAKLAR:
1. Controllers/TicketsController.cs:
   - Details: ticket + replies + attachments + audit events + time entries yükle
   - Create POST: IFormFileCollection ile dosya yükleme (max 10MB, 5 dosya)
   - Reply POST: dosya + IsInternal + TimeSpentMinutes
   - DownloadAttachment(int id): güvenli dosya indirme
   - AddTimeEntry POST: manuel zaman girişi
   - StartTimer / StopTimer (AJAX endpoint'leri)
   - EditTicket POST: başlık/açıklama/priority/category güncelleme (Support/Admin)
2. Services/FileAttachmentService.cs (yeni):
   - UploadAsync, DeleteAsync, ValidateFile
   - GUID dosya adı, orijinal adı DB'de
   - İzin verilen uzantılar: .pdf,.png,.jpg,.jpeg,.docx,.xlsx,.txt,.zip,.pptx
3. Views/Tickets/Details.cshtml: komple yeniden yaz
   - Sol kolon (8 birim):
     - Ticket başlığı + düzenle butonu (Support/Admin)
     - Açıklama kartı (markdown render)
     - Zaman çizelgesi: reply'lar + audit event'ler KARIŞIK kronolojik
     - Her reply: yazar, zaman, mesaj, dosya ekleri, "Dahili Not" badge
     - Cevap formu: textarea, dosya ekle, IsInternal checkbox, zaman girişi (dakika)
   - Sağ kolon (4 birim):
     - Durum badge'i + değiştirme dropdown'u
     - Öncelik + Kategori badge'leri
     - Atanan kişi + "Üstlen" butonu
     - Departman
     - SLA bilgisi: kalan süre / gecikme
     - Dosya ekleri listesi
     - Zaman takibi: toplam süre, timer başlat/durdur
     - Etiketler
4. wwwroot/css/ticket-detail.css (yeni): timeline stilleri
5. wwwroot/js/ticket-detail.js (yeni): timer, AJAX işlemleri
6. Views/Tickets/Create.cshtml güncelle:
   - enctype multipart/form-data
   - Dosya sürükle-bırak alanı
   - Template'den oluşturma (dropdown, varsa)
   - Priority + Category dropdown'ları
7. Program.cs: FileAttachmentService DI
   // Issue 2.3

KABUL:
- Dosya yükleme ve indirme çalışıyor
- Dahili notlar müşteriye gösterilmiyor
- Zaman takibi (manuel giriş)
- Audit timeline görünüyor
- Sürükle-bırak dosya yükleme
- SLA bilgisi detayda görünüyor
```

---

### Issue 2.4 — REST API + Swagger + API Key Auth
**PC:** Codex | **Bağımlılık:** Phase 1

```
ISSUE 2.4: REST API, Swagger Dokümantasyonu ve API Key Kimlik Doğrulaması

Tam kapsamlı REST API. API key auth. Swagger/OpenAPI docs.

YAPILACAKLAR:
1. Program.cs:
   - builder.Services.AddControllers() + JsonOptions (camelCase)
   - builder.Services.AddSwaggerGen() (Swashbuckle veya NSwag)
   - builder.Services.AddApiVersioning()
   - app.UseSwagger() + app.UseSwaggerUI()
   - API key authentication scheme ekle
2. Controllers/Api/V1/TicketsController.cs (yeni):
   - [ApiController, Route("api/v1/tickets"), Authorize]
   - GET / (list + filter + pagination)
   - GET /{id} (detail + replies + attachments)
   - POST / (create ticket) [Customer]
   - PUT /{id} (update ticket) [Support/Admin]
   - DELETE /{id} (soft delete) [Admin]
   - POST /{id}/reply (add reply)
   - PUT /{id}/status (change status) [Support/Admin]
   - PUT /{id}/assign (assign to self) [Support/Admin]
   - PUT /{id}/priority (change priority)
   - GET /{id}/attachments (list attachments)
   - POST /{id}/attachments (upload)
   - GET /{id}/audit (audit log)
3. Controllers/Api/V1/AccountController.cs (yeni):
   - GET /me (current user)
   - PUT /me (update profile)
   - POST /change-password
4. Controllers/Api/V1/KnowledgeBaseController.cs (yeni):
   - GET / (list articles) [AllowAnonymous]
   - GET /{id} (article detail) [AllowAnonymous]
5. Controllers/Api/V1/AdminController.cs (yeni):
   - GET /users [Admin]
   - PUT /users/{id} [Admin]
   - GET /stats [Admin]
6. Services/ApiKeyAuthHandler.cs (yeni):
   - AuthenticationHandler<ApiKeyAuthOptions>
   - Header: X-API-Key
   - DB'de hash'li key'i doğrula
7. Models/ApiResponse.cs (yeni): generic wrapper
   - Success, Data, Message, Errors, Pagination
8. Her endpoint için XML doc comments (Swagger'da görünsün)

KABUL:
- /swagger adresinde tam API dökümantasyonu
- API versioning (/api/v1/...)
- API key auth çalışıyor (header: X-API-Key)
- Tüm endpoint'ler JSON camelCase
- Pagination metadata response header'da
- Rate limiting API'de de geçerli
```

---

### Issue 2.5 — Email + Bildirim Sistemi
**PC:** OpenCode | **Bağımlılık:** Phase 1

```
ISSUE 2.5: Email Motoru ve Tam Bildirim Sistemi

SMTP email altyapısı, notification center, email template engine.

YAPILACAKLAR:
1. appsettings.json: "Smtp" section (Host, Port, EnableSsl, User, Pass, From, FromName, BaseUrl)
2. Services/IEmailService.cs (yeni): Task SendAsync(...)
3. Services/SmtpEmailService.cs (yeni):
   - System.Net.Mail ile SMTP
   - HTML email, UTF-8
   - Hata durumunda crash YOK, logla
   - Retry logic (3 deneme)
4. Services/EmailTemplateEngine.cs (yeni):
   - RazorLight veya string replace ile template işleme
   - Template'ler: Views/Emails/ klasöründe .cshtml
5. Services/NotificationService.cs (yeni):
   - IEmailService + IAppDataStore inject
   - Olay bazlı metodlar:
     - TicketCreated(ticket)
     - TicketAssigned(ticket, assignee)
     - NewReply(ticket, reply)
     - StatusChanged(ticket, oldStatus, newStatus)
     - SlaBreached(ticket)
     - TicketOverdue(ticket)
     - SurveyRequested(ticket)
     - MentionReceived(ticket, mentionedUser)
   - Fire-and-forget: _ = Task.Run(async () => { ... });
   - Kullanıcı tercihlerine saygılı (EmailNotificationsEnabled)
6. Email template'leri Views/Emails/:
   - TicketCreated.cshtml
   - TicketAssigned.cshtml
   - NewReply.cshtml
   - StatusChanged.cshtml
   - SlaWarning.cshtml
   - SatisfactionSurvey.cshtml
   - DailyDigest.cshtml
   - MentionNotification.cshtml
7. Services/DailyDigestService.cs (yeni):
   - Admin/Support'a günlük özet email
   - Açık ticket sayısı, SLA ihlalleri, bugün kapatılanlar
   - Hangfire veya benzeri scheduled job
8. Controllers/NotificationsController.cs (yeni):
   - [Authorize]
   - Index: kullanıcının bildirim listesi
   - MarkRead(int id): okundu işaretle
   - MarkAllRead: tümünü oku
   - GetUnreadCount: AJAX (header'daki bell icon için)
9. Views/Notifications/Index.cshtml (yeni): bildirim listesi
10. Program.cs: Email + Notification servisleri DI
    // Issue 2.5
11. docker-compose.yml: MailHog test servisi ekle

KABUL:
- Ticket oluşturma/atama/cevap email'leri gidiyor
- SLA ihlali uyarı email'i
- Bildirim merkezi sayfası
- Header'da bildirim bell icon'u (okunmamış sayısı)
- Daily digest email (manuel tetiklenebilir)
- Dahili notlar email GÖNDERMEZ
```

---

### Issue 2.6 — Kullanıcı Profili + Ayarlar + 2FA
**PC:** OpenCode | **Bağımlılık:** Phase 1

```
ISSUE 2.6: Kullanıcı Profili, Hesap Ayarları ve İki Faktörlü Doğrulama

Profil yönetimi, bildirim tercihleri, tema ayarları, 2FA.

YAPILACAKLAR:
1. Models/ProfileViewModel.cs (yeni): tüm profil alanları
2. Models/ChangePasswordViewModel.cs (yeni): CurrentPassword, NewPassword, ConfirmPassword
3. Models/NotificationSettingsViewModel.cs (yeni): event bazlı email/push toggle'lar
4. Models/AppearanceSettingsViewModel.cs (yeni): DarkMode, Language
5. Models/Setup2FaViewModel.cs (yeni): QR code data, verification code
6. Controllers/AccountController.cs genişlet:
   - Profile GET/POST: profil bilgileri + istatistikler
   - ChangePassword GET/POST: şifre değiştir
   - NotificationSettings GET/POST: bildirim tercihleri
   - AppearanceSettings GET/POST: tema/dil
   - Setup2FA GET: QR kod üret (TOTP)
   - Setup2FA POST: doğrulama kodu ile aktifleştir
   - Disable2FA POST
   - Login POST: 2FA aktifse doğrulama kodu sor
   - ExportMyData GET: GDPR veri dışa aktarımı
   - DeleteAccount POST: hesap silme (GDPR)
7. Services/TotpService.cs (yeni):
   - GenerateSecret(), GenerateQrCodeUri(), ValidateCode()
   - Otentikator uygulama uyumlu
8. Views/Account/Profile.cshtml (yeni)
9. Views/Account/ChangePassword.cshtml (yeni)
10. Views/Account/NotificationSettings.cshtml (yeni)
11. Views/Account/AppearanceSettings.cshtml (yeni)
12. Views/Account/Setup2FA.cshtml (yeni): QR kod gösterimi
13. Views/Account/Verify2FA.cshtml (yeni): login sonrası kod girişi
14. wwwroot/js/profile.js: tema toggle AJAX, anlık tema değişimi
15. _Layout.cshtml: dark mode CSS class, kullanıcı adı → Profile link

KABUL:
- Profil düzenleme çalışıyor
- Şifre değiştirme (mevcut şifre kontrolü)
- Bildirim tercihleri kaydediliyor
- Dark mode toggle (anlık değişim)
- 2FA kurulumu (QR kod + doğrulama)
- 2FA login akışı
- GDPR veri export
```

---

### Issue 2.7 — Admin Paneli
**PC:** OpenCode | **Bağımlılık:** Phase 1

```
ISSUE 2.7: Admin Paneli - Kullanıcı, Departman, Sistem Yönetimi

Full-featured admin panel.

YAPILACAKLAR:
1. Controllers/AdminController.cs (yeni):
   - [Authorize(Roles="Admin")]
   - Index: sistem dashboard'u
   - Users: kullanıcı listesi + arama + filtre
   - EditUser GET/POST: tüm alanları düzenle
   - CreateUser GET/POST: yeni kullanıcı oluştur (Admin tarafından)
   - ToggleUserStatus POST: aktif/pasif
   - ResetPassword POST: random şifre
   - Departments: departman listesi
   - EditDepartment GET/POST: CRUD
   - SlaPolicies: SLA politikaları listesi
   - EditSlaPolicy GET/POST
   - TicketTemplates: şablon listesi
   - EditTicketTemplate GET/POST: CRUD
   - CannedResponses: hazır yanıt listesi
   - EditCannedResponse GET/POST: CRUD
   - Tags: etiket yönetimi
   - EditTag GET/POST: CRUD
   - AuditLog: sistem geneli audit log arama/filtreleme
   - SystemSettings: appsettings'ten okunan ayarları görüntüle/düzenle
   - ApiKeys: API key listesi
   - CreateApiKey POST / RevokeApiKey POST
   - Webhooks: webhook listesi
   - EditWebhook GET/POST: CRUD
   - TestWebhook POST
   - RecurringTickets: yinelenen ticket'lar
2. Views/Admin/Index.cshtml: 8 istatistik kartı
3. Views/Admin/Users.cshtml + EditUser.cshtml + CreateUser.cshtml
4. Views/Admin/Departments.cshtml + EditDepartment.cshtml
5. Views/Admin/SlaPolicies.cshtml + EditSlaPolicy.cshtml
6. Views/Admin/TicketTemplates.cshtml + EditTicketTemplate.cshtml
7. Views/Admin/CannedResponses.cshtml + EditCannedResponse.cshtml
8. Views/Admin/Tags.cshtml + EditTag.cshtml
9. Views/Admin/AuditLog.cshtml
10. Views/Admin/SystemSettings.cshtml
11. Views/Admin/ApiKeys.cshtml
12. Views/Admin/Webhooks.cshtml + EditWebhook.cshtml
13. Views/Admin/RecurringTickets.cshtml
14. Views/Shared/_AdminLayout.cshtml (opsiyonel): admin sidebar
15. wwwroot/css/admin.css: admin panel stilleri

KABUL:
- Admin paneli sadece Admin rolü
- Tüm CRUD işlemleri çalışıyor
- Audit log arama/filtreleme
- API key yönetimi
- Webhook test
- Sidebar navigasyon
```

---

### Issue 2.8 — Bilgi Bankası + Servis Kataloğu
**PC:** OpenCode | **Bağımlılık:** Phase 1

```
ISSUE 2.8: Bilgi Bankası, SSS ve Servis Kataloğu

Müşterilerin kendi kendine yardım edebileceği knowledge base.

YAPILACAKLAR:
1. Controllers/KnowledgeBaseController.cs (yeni):
   - [AllowAnonymous] Index, Article, Search, Category
   - Index: kategoriler + popüler makaleler
   - Article(string slug): makale detayı
   - Search(string q): arama sonuçları
   - Category(string category): kategoriye göre filtrele
   - RateHelpful POST: faydalı butonu
   - RateNotHelpful POST: faydasız butonu
2. Views/KnowledgeBase/Index.cshtml (yeni):
   - Hero search bar (büyük, ortada)
   - Kategori kartları (grid)
   - Popüler makaleler (ViewCount'a göre)
   - Son eklenenler
3. Views/KnowledgeBase/Article.cshtml (yeni):
   - Markdown render (marked.js)
   - Breadcrumb
   - Meta: yazar, tarih, görüntülenme, okuma süresi
   - Faydalı / Faydasız butonları
   - İlgili makaleler (sidebar)
   - İçindekiler (otomatik heading'lerden)
4. Views/KnowledgeBase/Search.cshtml (yeni): arama sonuçları
5. Controllers/ServiceCatalogController.cs (yeni):
   - [Authorize] Index, Request
   - Index: servis kataloğu (kart grid)
   - Request(int id): servis talebi → otomatik ticket oluştur
6. Models/ServiceCatalogItem.cs (yeni):
   - Id, Name, Description, Icon, Category, DefaultPriority, DefaultCategory, IsActive, SortOrder
7. Data/ApplicationDbContext.cs: ServiceCatalogItem DbSet (+ migration YOK, en son Phase 1'de eklenmiş olmalı)
   NOT: Bu entity Phase 1.6'da eklenmemişse şimdi migration GEREKMEZ - JSON dosyadan oku veya DB'ye ekle.
   Alternatif: appsettings.json'dan "ServiceCatalog" section'ı oku.
8. Views/ServiceCatalog/Index.cshtml (yeni): kart grid
9. Services/DatabaseSeeder.cs: 5+ bilgi bankası makalesi, 6 servis kataloğu öğesi
10. wwwroot/js/knowledge-base.js: search autocomplete, markdown render

KABUL:
- Bilgi bankası login gerektirmez
- Markdown render ediliyor
- Faydalı/faydasız oylama
- Arama sonuçları
- İlgili makaleler
- Servis kataloğundan ticket oluşturma
```

---

### Issue 2.9 — SignalR Gerçek Zamanlı Bildirimler
**PC:** Codex | **Bağımlılık:** Phase 1

```
ISSUE 2.9: Gerçek Zamanlı Bildirimler (SignalR)

WebSocket ile canlı güncellemeler, toast notification'lar.

YAPILACAKLAR:
1. TicketSupport.csproj: Microsoft.AspNetCore.SignalR (built-in)
2. Hubs/TicketHub.cs (yeni):
   - TicketUpdated, TicketAssigned, NewReply, StatusChanged
   - SlaWarning, MentionNotification
   - JoinTicketGroup, LeaveTicketGroup
3. Hubs/NotificationHub.cs (yeni):
   - SendNotification, MarkAsRead
   - User-specific notifications
4. wwwroot/js/signalr-client.js (yeni):
   - SignalR bağlantı yönetimi
   - Toast notification gösterimi
   - Ticket detay sayfasında canlı güncelleme
   - Header notification count güncelleme
5. Services/RealTimeNotificationService.cs (yeni):
   - IHubContext<TicketHub> inject
   - Tüm notification event'lerini hub üzerinden broadcast
6. Services/NotificationService.cs güncelle:
   - Email'e EK OLARAK SignalR notification da gönder
7. Program.cs:
   - builder.Services.AddSignalR()
   - app.MapHub<TicketHub>("/hubs/tickets")
   - app.MapHub<NotificationHub>("/hubs/notifications")
   // Issue 2.9
8. _Layout.cshtml:
   - signalr-client.js script
   - Toast container div
   - Header bell icon + badge
9. wwwroot/css/toast.css (yeni): toast notification stilleri
10. Views/Shared/_ToastPartial.cshtml: toast template

KABUL:
- Yeni cevap gelince detay sayfası canlı güncelleniyor
- Ticket atanınca support ekranında toast
- Header bildirim sayacı canlı
- SLA uyarıları toast olarak geliyor
- Sayfa refresh gerekmeden güncelleme
```

---

### Issue 2.10 — SLA Motoru + Otomatik Eskalasyon
**PC:** OpenCode | **Bağımlılık:** Phase 1

```
ISSUE 2.10: SLA Takip Motoru ve Otomatik Eskalasyon

SLA deadline hesaplama, overdue tespiti, otomatik eskalasyon.

YAPILACAKLAR:
1. Services/SlaEngine.cs (yeni):
   - CalculateDueDate(priority, createdAt): SLA policy'den hesapla
   - IsOverdue(ticket): due date geçmiş mi kontrol et
   - GetSlaStatus(ticket): OnTrack, AtRisk, Breached
   - CalculateResponseTime(ticket): ilk cevap süresi
   - CalculateResolutionTime(ticket): çözüm süresi
   - GetRemainingTime(ticket): kalan süre (human readable)
   - BusinessHoursOnly: iş saatleri içinde hesaplama (8-18, haftaiçi)
2. Services/SlaEscalationService.cs (yeni):
   - CheckAndEscalate(ticket):
     - Overdue + 0 eskalasyon → Support'a email
     - Overdue + 1 eskalasyon → Departman lead'e email
     - Overdue + 2 eskalasyon → Admin'e email
     - Her eskalasyonda EscalationLevel arttır
   - RecalculateAllDueDates(): sistem başlangıcında
3. Services/BackgroundSlaChecker.cs (yeni):
   - IHostedService implementasyonu
   - Her 5 dakikada bir tüm açık ticket'ları kontrol et
   - Overdue olanları işaretle, eskalasyon tetikle
4. Models/SupportTicket.cs: IsOverdue hesaplaması (computed, DB'de değil)
5. Views/Tickets/Index.cshtml: SLA durum kolonu (yeşil/sarı/kırmızı)
6. Views/Tickets/Details.cshtml: SLA sayacı (geri sayım veya gecikme)
7. Controllers/SlaController.cs (yeni): [Admin] SLA raporları
8. Views/Sla/Index.cshtml: SLA compliance dashboard
9. wwwroot/js/sla-timer.js: canlı SLA geri sayım (client-side)

KABUL:
- Priority'ye göre due date hesaplanıyor
- Overdue ticket'lar kırmızı vurgulanıyor
- Otomatik eskalasyon email'leri
- SLA compliance raporu
- İş saati hesaplaması
- Background service her 5 dk kontrol
```

### ✅ PHASE 2 CHECKPOINT

---

## PHASE 3 — Advanced & Polish (PARALEL)

### Issue 3.1 — Webhook + Entegrasyon Altyapısı
**PC:** Codex | **Bağımlılık:** Phase 2

```
ISSUE 3.1: Webhook Sistemi ve Dış Entegrasyonlar

Outgoing webhook'lar, Slack/Teams entegrasyonu.

YAPILACAKLAR:
1. Services/WebhookDispatcher.cs (yeni):
   - DispatchAsync(eventType, payload): tüm aktif subscription'lara gönder
   - HMAC-SHA256 signature header'ı
   - Retry logic (exponential backoff, max 3)
   - Failed log
   - HttpClientFactory kullan
2. Services/WebhookPayloadBuilder.cs (yeni): event-specific JSON payload
3. Services/NotificationService.cs güncelle: email + SignalR + WEBOOK
4. Services/SlackIntegrationService.cs (yeni):
   - Slack incoming webhook
   - Ticket olaylarını Slack kanalına gönder
   - Formatlı message blocks
5. appsettings.json: "Integrations:Slack:WebhookUrl" ekle
6. Controllers/AdminController.cs Webhook test butonu

KABUL:
- Webhook'lar ticket olaylarında tetikleniyor
- HMAC signature doğru
- Retry çalışıyor
- Slack notification formatlı
```

---

### Issue 3.2 — Raporlama + PDF Export + Zamanlanmış Raporlar
**PC:** OpenCode | **Bağımlılık:** Phase 2

```
ISSUE 3.2: Gelişmiş Raporlama, PDF Export ve Zamanlanmış Raporlar

Dashboard ötesi raporlama, PDF çıktı, email ile otomatik rapor.

YAPILACAKLAR:
1. Controllers/ReportsController.cs (yeni):
   - [Authorize(Roles="Support,Admin")]
   - Index: rapor seçim sayfası
   - TicketVolume: dönem seç, bar chart + tablo
   - SlaCompliance: SLA uyum yüzdesi
   - AgentPerformance: support başına metrikler
   - CategoryTrend: kategori trend grafiği
   - CustomerSatisfaction: memnuniyet ortalamaları
   - ExportPdf: her raporu PDF olarak indir
   - ExportCsv: CSV olarak indir
2. Views/Reports/Index.cshtml: rapor kartları
3. Views/Reports/*.cshtml: her rapor sayfası (Chart.js)
4. Services/PdfReportService.cs (yeni):
   - IronPdf, DinkToPdf veya PuppeteerSharp ile HTML→PDF
   - Rapor template'leri
5. Services/ScheduledReportService.cs (yeni):
   - IHostedService: cron schedule ile çalışır
   - Haftalık/daily admin email raporu
   - PDF ek olarak email
6. wwwroot/js/reports.js: Chart.js rapor grafikleri

KABUL:
- En az 5 farklı rapor tipi
- PDF export
- CSV export (güncellenmiş)
- Haftalık rapor email'i
```

---

### Issue 3.3 — Unit + Integration Tests
**PC:** Codex | **Bağımlılık:** Phase 2 core

```
ISSUE 3.3: Kapsamlı Test Projesi

Unit test + integration test. 50+ test.

YAPILACAKLAR:
1. Test projesi oluştur: TicketSupport.Tests (xUnit)
2. NuGet: Moq, FluentAssertions, EF Core InMemory, Microsoft.AspNetCore.Mvc.Testing
3. Unit test sınıfları:
   - PasswordHasherTests (5 test)
   - SlaEngineTests (8 test): due date, overdue, business hours
   - PostgresAppDataStoreTests (15 test): CRUD + filtreleme
   - TicketQueryServiceTests (5 test): filtreleme logic
4. Controller testleri:
   - TicketsControllerTests (10 test): Moq ile
   - AccountControllerTests (5 test)
   - AdminControllerTests (3 test)
5. Integration testleri (WebApplicationFactory):
   - BasicAuthFlowTests (3 test): login, access protected page, logout
   - TicketLifecycleTests (3 test): create → assign → reply → close
   - ApiEndpointTests (3 test): GET/POST/PUT
6. Test helpers: TestDataFactory, MockHelpers
7. README.md'ye test komutları ekle

KABUL:
- dotnet test → 50+ test, hepsi yeşil
- Unit + integration testler
- CI-ready (tek komutla çalışır)
```

---

### Issue 3.4 — Güvenlik + Rate Limiting + Input Validation
**PC:** Codex | **Bağımlılık:** Phase 2

```
ISSUE 3.4: Güvenlik Sertleştirmesi, Rate Limiting, Gelişmiş Validasyon

OWASP best practices, anti-forgery, CSP, rate limiting, FluentValidation.

YAPILACAKLAR:
1. Rate limiting (ASP.NET Core built-in):
   - Login: 5/15dk/IP
   - Register: 3/saat/IP
   - API: 100/dk/key
   - Ticket create: 10/dk/user
2. Security headers middleware:
   - X-Content-Type-Options: nosniff
   - X-Frame-Options: DENY
   - X-XSS-Protection: 1; mode=block
   - Referrer-Policy: strict-origin-when-cross-origin
   - Content-Security-Policy
   - Permissions-Policy
   - Strict-Transport-Security (HSTS)
3. Cookie security: HttpOnly, Secure, SameSite=Strict, 2h expire
4. FluentValidation entegrasyonu:
   - TicketSupport.csproj: FluentValidation.AspNetCore
   - Validators/LoginViewModelValidator.cs
   - Validators/RegisterViewModelValidator.cs
   - Validators/CreateTicketViewModelValidator.cs
   - Validators/TicketReplyViewModelValidator.cs
   - Validators/ChangePasswordViewModelValidator.cs
   - Program.cs: .AddFluentValidationAutoValidation()
5. Anti-forgery: tüm POST form'larda [ValidateAntiForgeryToken] kontrolü
6. Input sanitization: HtmlEncode tüm kullanıcı girdilerine
7. SQL injection: EF Core parametrized queries zaten koruyor (verify)
8. File upload security:
   - Extension whitelist
   - MIME type validation (magic bytes)
   - Max file size
   - Virus scan? (opsiyonel, ClamAV entegrasyonu)
9. Brute force protection: login'de ardışık başarısız deneme → captcha
10. Session security: concurrent session limit
11. Security.txt ekle
12. .gitignore: hassas dosyalar kontrolü

KABUL:
- OWASP ZAP taraması temel seviye
- Rate limiting aktif
- Security header'ları tüm response'larda
- FluentValidation çalışıyor
- File upload güvenli
```

---

### Issue 3.5 — UI/UX Cilalama + Dark Mode + PWA
**PC:** OpenCode | **Bağımlılık:** Phase 2

```
ISSUE 3.5: UI/UX Cilalama, Dark Mode, PWA ve Erişilebilirlik

Production-quality UI. Dark mode. PWA desteği. A11Y.

YAPILACAKLAR:
1. Dark mode:
   - CSS custom properties (variables) ile tema sistemi
   - data-bs-theme="dark" Bootstrap 5.3+ desteği
   - Toggle buton (header'da ay/hilal icon)
   - LocalStorage'da tercih saklama
   - Sistem tercihini otomatik algılama (prefers-color-scheme)
2. PWA:
   - wwwroot/manifest.json
   - wwwroot/service-worker.js (offline caching)
   - 192x192 + 512x512 icon'lar
   - <link rel="manifest"> + <meta name="theme-color">
3. Erişilebilirlik (WCAG 2.1 AA):
   - ARIA label'ları tüm interaktif elemanlarda
   - Klavye navigasyonu (focus visible)
   - Skip-to-content linki
   - Yeterli renk kontrastı
   - Screen reader dostu form elemanları
   - alt metinleri tüm img'lerde
4. UI iyileştirmeleri:
   - Toast notification sistemi (TempData yerine)
   - Skeleton loading (placeholder shimmer)
   - Smooth page transitions
   - Tooltip'ler
   - Onay dialog'ları (SweetAlert2 veya Bootstrap modal)
   - Empty state illüstrasyonları
   - 404/500 özel sayfaları
   - Breadcrumb navigasyonu
5. Performans:
   - CSS/JS minification (build-time)
   - Image lazy loading
   - Font subsetting (Türkçe karakterler)
   - Response caching
6. Mobil:
   - Bottom navigation bar (mobile)
   - Swipe gestures (ticket list)
   - Pull-to-refresh
7. wwwroot/css/theme.css: tema değişkenleri
8. wwwroot/css/utilities.css: helper sınıflar
9. wwwroot/js/app.js: global UI utilities

KABUL:
- Dark mode çalışıyor (toggle + sistem algılama)
- PWA installable (manifest + service worker)
- Klavye ile navigate edilebiliyor
- Lighthouse score > 90
- Mobil-first responsive
- WCAG temel uyumluluk
```

---

### Issue 3.6 — DevOps + Docker Prod + Logging + Health Checks
**PC:** Codex | **Bağımlılık:** Phase 2

```
ISSUE 3.6: Production DevOps, Serilog, Health Checks, Docker Build

Production'a hazır hale getir.

YAPILACAKLAR:
1. Serilog entegrasyonu:
   - TicketSupport.csproj: Serilog.AspNetCore, Serilog.Sinks.Console, Serilog.Sinks.File, Serilog.Sinks.PostgreSQL
   - Program.cs: builder.Host.UseSerilog()
   - appsettings.json: Serilog config (minimum level, sinks)
   - Tüm controller'larda ILogger<T> inject
   - Kritik noktalarda log (login, ticket create, status change, error)
2. Health Checks:
   - Program.cs: builder.Services.AddHealthChecks()
     .AddNpgSql(...)  // DB health
     .AddUrlGroup(...) // external dependency'ler
   - app.MapHealthChecks("/health")
   - app.MapHealthChecks("/health/ready") // readiness
   - app.MapHealthChecks("/health/live") // liveness
3. Docker production:
   - Dockerfile (multi-stage build):
     - Stage 1: SDK ile build + publish
     - Stage 2: ASP.NET runtime image
     - Optimize katmanlar (csproj önce kopyala, sonra src)
   - docker-compose.prod.yml:
     - app servisi (Dockerfile'dan build)
     - postgres servisi
     - nginx reverse proxy (opsiyonel)
     - MailHog (sadece staging)
   - .dockerignore
4. Environment yapılandırması:
   - appsettings.Development.json (güncelle)
   - appsettings.Staging.json (yeni)
   - appsettings.Production.json (yeni)
   - Environment variable override desteği
5. Nginx reverse proxy config (opsiyonel):
   - nginx.conf: SSL termination, static files, proxy pass
6. Database backup script:
   - Scripts/backup-db.sh
   - pg_dump ile yedek
   - Zamanlanmış cron
7. Build script:
   - Scripts/build.sh: dotnet build + test + publish
8. README.md güncelle: production deployment talimatları

KABUL:
- Serilog tüm log'ları yakalıyor
- /health endpoint'i çalışıyor
- Docker multi-stage build başarılı
- docker-compose.prod.yml ile production başlatma
- Environment-specific config dosyaları
```

---

### Issue 3.7 — JSON File Data Store
**PC:** OpenCode | **Bağımlılık:** Phase 2

```
ISSUE 3.7: JSON Dosya Tabanlı Alternatif Veri Deposu

PostgreSQL olmadan da çalışabilen JSON storage.

YAPILACAKLAR:
1. Services/JsonDataContext.cs (yeni):
   - App_Data/ altında tüm entity'ler için ayrı JSON dosyaları
   - ReaderWriterLockSlim ile thread-safe
   - ID counter yönetimi
   - Lazy loading + dirty tracking
2. Services/JsonAppDataStore.cs (yeni):
   - IAppDataStore full implementasyonu
   - LINQ-to-Objects ile filtreleme
   - Transaction safety
3. Program.cs: "DataStore" config'e göre DI
   - "Postgres" (default): EF Core
   - "Json": JSON dosya
4. appsettings.json: "DataStore": "Postgres"
5. App_Data/ klasörünü .gitignore'a ekle

KABUL:
- JSON modunda PostgreSQL olmadan çalışıyor
- Tüm API ve MVC feature'ları çalışıyor
- Config switch ile geçiş
- Thread-safe
```

---

### Issue 3.8 — Memnuniyet Anketi + Ticket Kapatma Akışı
**PC:** OpenCode | **Bağımlılık:** Phase 2

```
ISSUE 3.8: Müşteri Memnuniyet Anketi ve Ticket Kapatma Akışı

Ticket çözüldükten sonra otomatik anket. Kapatma workflow'u.

YAPILACAKLAR:
1. Services/SatisfactionSurveyService.cs (yeni):
   - Ticket Solved/Closed olduğunda otomatik anket email'i
   - Unique survey link (GUID token)
   - Anket sayfası (public, token ile)
   - Sonuçları kaydet, dashboard'da göster
2. Controllers/SurveyController.cs (yeni):
   - [AllowAnonymous] Index(string token): anket formu
   - Submit POST: rating + comment kaydet
   - ThankYou: teşekkür sayfası
3. Views/Survey/Index.cshtml: 1-5 yıldız + yorum
4. Views/Survey/ThankYou.cshtml
5. Ticket kapatma workflow'u:
   - Support "Çözüldü" yapınca → 48 saat bekle → "Kapandı" otomatik
   - Customer itiraz ederse → "Açık" a geri dön
   - WaitingCustomer status: müşteriden bilgi bekleniyor
6. Services/TicketClosingService.cs (yeni):
   - IHostedService: her saat kontrol
   - 48 saattir Solved olanları Closed yap
7. Views/Tickets/Details.cshtml: kapatma/itiraz butonları

KABUL:
- Çözülen ticket'lar için anket email'i gidiyor
- Anket sayfası login gerektirmez
- 48 saat sonra otomatik kapanma
- Customer itiraz edebiliyor
- WaitingCustomer status'u var
```

---

### Issue 3.9 — Final README + Dokümantasyon + Demo Data
**PC:** Codex | **Bağımlılık:** Tüm phase'ler

```
ISSUE 3.9: Final README, Tam Demo Veri ve Dokümantasyon

Her şeyi belgele, demo veriyi zenginleştir.

YAPILACAKLAR:
1. README.md: sıfırdan yaz
   - Proje adı ve açıklaması
   - Özellik listesi (tüm implemente edilenler)
   - Teknoloji stack
   - Mimari genel bakış (klasör yapısı + design patterns)
   - Kurulum adımları:
     a. git clone
     b. docker compose up -d
     c. dotnet run
     d. Tarayıcıda aç
   - Demo hesaplar tablosu
   - Konfigürasyon rehberi (appsettings.json)
   - API dokümantasyonu özeti
   - Test çalıştırma
   - Production deployment (docker)
2. DatabaseSeeder.cs: EPIC demo veri
   - Admin, 3 support (farklı departman), 5 customer (2 organizasyon)
   - 15+ ticket (tüm status/priority/category kombinasyonları)
   - Her ticket'a cevaplar (dahili not dahil)
   - Audit event'ler
   - 8 bilgi bankası makalesi (her kategoriden)
   - 3 ticket template
   - 5 canned response
   - SLA politikaları (4 priority için)
   - 3 webhook subscription (1 aktif, 2 pasif)
   - 10+ etiket
   - 2 API key
   - Memnuniyet anketleri
3. .editorconfig: C# code style
4. CONTRIBUTING.md: geliştirici rehberi
5. CHANGELOG.md: sürüm geçmişi
6. LICENSE

KABUL:
- README tek sayfada tüm sistemi anlatıyor
- Demo veri tüm feature'ları sergiliyor
- Sıfırdan klonlayıp çalıştırılabiliyor
- docker compose up -d → dotnet run → tüm özellikler gezilebilir
```

### ✅ PHASE 3 CHECKPOINT

---

## ÖZET TABLO

| Faz | Issue | Başlık | PC |
|-----|-------|--------|-----|
| **Phase 1** | 1.1 | BCrypt + Güvenlik Altyapısı | Codex |
| (Sequential) | 1.2 | Admin + Departman | Codex |
| | 1.3 | Priority/Category/Tag/SLA | Codex |
| | 1.4 | AppUser + Organizasyon | Codex |
| | 1.5 | Reply/Note/CannedResponse/Satisfaction | Codex |
| | 1.6 | Audit/Knowledge/Webhook/Template (SON MIGRATION) | Codex |
| **Phase 2** | 2.1 | Arama/Filtre/Sayfalama | Codex |
| (Parallel) | 2.2 | Dashboard + SLA Paneli | OpenCode |
| | 2.3 | Ticket Detay + Dosya + Zaman | Codex |
| | 2.4 | REST API + Swagger | Codex |
| | 2.5 | Email + Bildirim Sistemi | OpenCode |
| | 2.6 | Profil + Ayarlar + 2FA | OpenCode |
| | 2.7 | Admin Paneli | OpenCode |
| | 2.8 | Bilgi Bankası + Servis Kataloğu | OpenCode |
| | 2.9 | SignalR Gerçek Zamanlı | Codex |
| | 2.10 | SLA Motoru + Eskalasyon | OpenCode |
| **Phase 3** | 3.1 | Webhook + Entegrasyon | Codex |
| (Parallel) | 3.2 | Raporlama + PDF Export | OpenCode |
| | 3.3 | Unit + Integration Tests | Codex |
| | 3.4 | Güvenlik + Rate Limiting | Codex |
| | 3.5 | UI/UX + Dark Mode + PWA | OpenCode |
| | 3.6 | DevOps + Docker Prod + Logging | Codex |
| | 3.7 | JSON Data Store | OpenCode |
| | 3.8 | Memnuniyet Anketi + Kapatma | OpenCode |
| | 3.9 | Final README + Demo Data | Codex |

**Toplam:** 25 issue | **Codex:** 14 | **OpenCode:** 11

## Paralel Çalışma Grupları

```
Phase 1: Codex sırayla 1.1→1.2→1.3→1.4→1.5→1.6
         |
Phase 2: ├── Codex: 2.1 → 2.3 → 2.4 → 2.9 (sırayla ama OpenCode'tan bağımsız)
         └── OpenCode: 2.2 → 2.5 → 2.6 → 2.7 → 2.8 → 2.10 (sırayla ama Codex'ten bağımsız)
         |
Phase 3: ├── Codex: 3.1 → 3.3 → 3.4 → 3.6 → 3.9
         └── OpenCode: 3.2 → 3.5 → 3.7 → 3.8
```

## Merge Çakışması Önleme

- **Migration'lar:** SADECE Phase 1'de. Phase 2+3'te migration YOK
- **Program.cs:** Her issue kendi `// Issue X.Y` yorum BLOĞUNA ekleme yapar
- **Controller'lar:** Her issue ya yeni controller ekler ya da mevcut controller'a metod ekler (farklı metod isimleri)
- **View'lar:** Çoğu issue yeni view dosyası; Index.cshtml ve Details.cshtml sadece Codex değiştirir
- **Shared dosyalar:** _Layout.cshtml'ye eklemeler farklı bölgelere (nav, scripts, css)
