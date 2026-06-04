# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Start PostgreSQL and MailHog
docker compose up -d

# Run the application (migrations apply automatically, seed data populated)
dotnet run

# EF Core migrations
dotnet ef migrations add MigrationName
dotnet ef database update
```

The app runs at `https://localhost:7215` (HTTPS) and `http://localhost:5076` (HTTP). Swagger UI is at `/swagger`.

No test project exists yet.

## Architecture

This is a **help desk / ticket management system** — ASP.NET Core MVC on **.NET 10**, EF Core with PostgreSQL, Turkish-language UI.

### Authentication & Authorization

- **Cookie auth** with a custom `AppAuth` policy scheme that selects between Cookie and API Key (the `ApiKeyAuthHandler` checks for `X-API-Key` header; API keys are stored hashed with BCrypt, verified in-memory against all active keys).
- **Three roles** (`UserRole` enum): `Customer` (1), `Support` (2), `Admin` (3).
- Claims include `NameIdentifier` (user ID), `Name`, `Email`, `Role`.
- API keys are created by admins, scoped with string scopes (e.g. `"read:tickets,read:reports"`), support expiration, and track `LastUsedAt`.

### Data Layer

- **`ApplicationDbContext`** (EF Core, PostgreSQL via `Npgsql`) — overrides `SaveChanges` to auto-set `CreatedAt`/`LastUpdatedAt` on `SupportTicket` and `KnowledgeArticle` via `UpdateAuditFields()`.
- **`IAppDataStore`** / **`PostgresAppDataStore`** — abstraction layer over the DbContext for MVC controller operations (query, create, assign, reply, update status, dashboard data). Injects `PasswordHasher` and `TicketQueryService`.
- **API controllers** (`Controllers/Api/V1/`) bypass `IAppDataStore` and use `ApplicationDbContext` directly for finer control.
- **Soft delete** on tickets: `IsDeleted` + `DeletedAt` fields; all queries filter `!IsDeleted`.
- **`DatabaseSeeder`** — idempotent seeding for dev/demo: admin, support, and customer users, organizations, departments, SLA policies, tags, sample tickets, knowledge base articles, templates, API keys, etc.

### Password Hashing

- **BCrypt** with pepper (`Security:PasswordPepper` from config), work factor 12, `$2b$` prefix.
- `PostgresAppDataStore.IsPasswordValid` supports transparent migration from legacy SHA256 hashes to BCrypt.

### Real-time (SignalR)

Two hubs under `/hubs/`:
- **`TicketHub`** — group-based (`ticket-{id}`); clients join/leave to receive `TicketUpdated`, `TicketAssigned`, `NewReply`, `StatusChanged` events.
- **`NotificationHub`** — per-user notifications: `NotificationReceived`, `NotificationCountUpdated`, `NotificationMarkedAsRead`, `SlaWarning`, `MentionNotification`.

**`RealTimeNotificationService`** wraps both hub contexts for broadcasting. **`NotificationService`** is the primary facade — it fire-and-forgets notification work via `Task.Run`, creates `UserNotification` DB records, sends emails via SMTP, and pushes real-time events. It uses `IServiceScopeFactory` to create scoped DbContext instances per background operation.

### MVC vs API

- **MVC controllers**: `AccountController`, `TicketsController`, `DashboardController`, `AdminController`, `KnowledgeController`, `HomeController`, `NotificationsController`. Use `IAppDataStore` abstraction, return Razor views.
- **API controllers** (`Controllers/Api/V1/`): `TicketsController`, `AccountController`, `AdminController`, `KnowledgeBaseController`. Inherit from `ApiControllerBase` which provides `CurrentUserId`, `CurrentUserRole`, `CurrentUserName`, `WritePaginationHeader`, and `ValidationProblemResponse`. Return `ApiResponse<T>` JSON envelope. API versioning via `Asp.Versioning.Mvc` (v1.0).
- **`ApiMappings`** (internal static) — extension methods mapping domain models to DTOs (`ToSummaryDto`, `ToDetailDto`, `ToDto`, etc.).
- Rate limiting on API routes (fixed window, 60 req/min).

### Key Services

| Service | Role |
|---|---|
| `PasswordHasher` | BCrypt hash/verify with pepper |
| `PostgresAppDataStore` | Primary data access for MVC controllers |
| `TicketQueryService` | Filter/sort logic for ticket queries (ILike search, date range, tags, overdue, etc.) |
| `NotificationService` | Fire-and-forget notification dispatch (email + in-app + real-time) |
| `RealTimeNotificationService` | SignalR broadcast wrapper |
| `FileAttachmentService` | Attachment upload/delete to `App_Data/uploads/` |
| `SmtpEmailService` | SMTP email sender (MailHog in dev) |
| `EmailTemplateEngine` | Razor-based email template rendering |
| `DailyDigestService` | `IHostedService` for scheduled daily digest |
| `ApiKeyAuthHandler` | Custom `AuthenticationHandler` for `X-API-Key` header |
| `SimpleMarkdown` | Minimal markdown-to-HTML renderer for ticket descriptions |

### Helpers

- **`DateTimeExtensions`** — converts UTC dates to Turkey local time (`Europe/Istanbul`) for display. All DB storage is UTC.
- **`DisplayLabels`** — Turkish labels and CSS classes for enums (`TicketPriority`, `TicketCategory`, `UserRole`, `TicketStatus`).

### Key Design Patterns

- **Fire-and-forget notifications**: `NotificationService` public methods fire `Task.Run` and swallow exceptions (logs to `ILogger`). The MVC `TicketsController` also calls `await`-able async methods (`NotifyTicketCreatedAsync`, `NotifyNewReplyAsync`, etc.) which combine DB persistence + real-time broadcasting + email in one call.
- **Ticket access control**: `CanAccessTicket` / `CanSeeTicket` — support and admin see all, customers see only their own tickets. Internal replies and their attachments are hidden from customers.
- **Template system**: `TicketTemplate` records define reusable ticket descriptions with default priority/category/department; customers select a template on creation.
- **SLA**: `SlaPolicy` records keyed by priority define response and resolution time targets. Ticket detail view computes SLA deadlines and displays warnings.
- **Time tracking**: Support users can log time entries (`TicketTimeEntry`), use a start/stop timer (stored in `IMemoryCache`), and see accumulated tracked minutes.
- **Audit trail**: `TicketAuditEvent` records every mutate action (create, reply, assign, status change, priority change, edit, delete) with actor, old/new values, and IP address.

### Configuration

- `appsettings.json` — connection string, password pepper, SMTP settings (MailHog defaults), `Smtp:BaseUrl` for email link generation.
- `appsettings.Development.json` — overrides connection string and logging.
- `docker-compose.yml` — PostgreSQL 16 + MailHog (SMTP on 1025, web UI on 8025).

### Demo Accounts

| Role | Email | Password |
|---|---|---|
| Admin | admin@ticket.local | 123456 |
| Support | destek@ticket.local | 123456 |
| Customer | musteri@ticket.local | 123456 |
