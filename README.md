# Ticket Destek Sistemi

ASP.NET Core MVC ile yazılmış teknik destek / ticket sistemi.

## Kullanılanlar

- ASP.NET Core MVC
- Cookie Authentication
- Role-based Authorization
- EF Core Code-First
- PostgreSQL
- Data Annotations validation

## Çalıştırma

1. PostgreSQL'i başlatın:

```bash
docker compose up -d
```

2. Uygulamayı çalıştırın:

```bash
dotnet run
```

Uygulama açılırken migration otomatik uygulanır ve demo kullanıcılar eklenir.

## Demo Kullanıcılar

Müşteri:

```text
musteri@ticket.local
123456
```

Destek ekibi:

```text
destek@ticket.local
123456
```
