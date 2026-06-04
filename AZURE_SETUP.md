# Azure Kurulum Rehberi

Bu rehber, Ticket Support uygulamasını Azure App Service + PostgreSQL Flexible Server
üzerinde çalıştırmak için gerekli adımları içerir.

## Ön Koşullar

- [Azure CLI](https://docs.microsoft.com/tr-tr/cli/azure/install-azure-cli) kurulu ve
  `az login` ile Student/Azure hesabına giriş yapılmış olmalı.
- GitHub repo public ve `release` branch'i mevcut.

## 0. Değişkenleri Ayarla

```bash
cp .env.example .env
# .env dosyasındaki değerleri kendine göre doldur (özellikle DB_PASSWORD ve PEPPER)
```

Sonra her adımdan önce değişkenleri yükle:

```bash
set -a && source .env && set +a
```

---

## 1. Azure Kaynaklarını Oluştur

```bash
set -a && source .env && set +a

# Kaynak grubu
az group create --name $RESOURCE_GROUP --location $LOCATION

# App Service Plan (Linux, B1 — student credits ile ücretsiz)
az appservice plan create \
  --name $APP_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# App Service (.NET 10)
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_PLAN \
  --runtime "DOTNETCORE:10.0"

# PostgreSQL Flexible Server (sifre kriterleri: buyuk/kucuk harf + rakam + ozel karakter)
az postgres flexible-server create \
  --name $DB_SERVER \
  --resource-group $RESOURCE_GROUP \
  --admin-user $DB_USER \
  --admin-password "$DB_PASSWORD" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --public-access 0.0.0.0

# Veritabanini olustur
az postgres flexible-server db create \
  --server-name $DB_SERVER \
  --resource-group $RESOURCE_GROUP \
  --database-name $DB_NAME

# Azure servislerinden gelen baglantilara izin ver (App Service -> PostgreSQL)
az postgres flexible-server firewall-rule create \
  --rule-name AllowAllAzureServices \
  --name $DB_SERVER \
  --resource-group $RESOURCE_GROUP \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 255.255.255.255
```

---

## 2. App Service Yapılandırması

```bash
set -a && source .env && set +a

DB_HOST="${DB_SERVER}.postgres.database.azure.com"

az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ConnectionStrings__DefaultConnection="Host=${DB_HOST};Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}" \
    Security__PasswordPepper="${PEPPER}" \
    ASPNETCORE_ENVIRONMENT="Production" \
    Smtp__BaseUrl="https://${APP_NAME}.azurewebsites.net"

# WebSocket etkinleştir (SignalR bildirimleri için onemli)
az webapp config set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --web-sockets-enabled true
```

---

## 3. OIDC Federasyonu (GitHub Actions ↔ Azure)

Bu adım GitHub Actions'ın Azure'a **secret olmadan** authenticate olmasını sağlar.

### 3a. App Registration ve Tenant ID

```bash
set -a && source .env && set +a

AZURE_SUBSCRIPTION_ID=$(az account show --query id -o tsv)
AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)

# App Registration oluştur
AZURE_CLIENT_ID=$(az ad app create --display-name "ticket-system-oidc" --query appId -o tsv)

echo "AZURE_CLIENT_ID=${AZURE_CLIENT_ID}"
echo "AZURE_TENANT_ID=${AZURE_TENANT_ID}"
echo "AZURE_SUBSCRIPTION_ID=${AZURE_SUBSCRIPTION_ID}"

# Bu cikan 3 degeri .env dosyasina kaydet
```

Çıkan 3 değeri `.env` dosyana yaz, sonra `.env.example`'ı da güncelle (başkaları için referans olması açısından ama gerçek değerleri koyma):

```
AZURE_CLIENT_ID="abc123..."
AZURE_TENANT_ID="def456..."
AZURE_SUBSCRIPTION_ID="ghi789..."
```

### 3b. Federated Credential (Azure Portal)

Bu kısmı Azure Portal'dan yapman gerek:

1. [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations**
2. Oluşturduğun `ticket-system-oidc` uygulamasını seç
3. **Certificates & secrets** → **Federated credentials** → **Add credential**
4. Aşağıdaki bilgileri gir:
   - **Scenario**: GitHub Actions deploying Azure resources
   - **Organization**: `.env`'deki `GITHUB_ORG` değeri
   - **Repository**: `.env`'deki `GITHUB_REPO` değeri
   - **Entity type**: `Branch`
   - **GitHub branch name**: `release`
   - **Name**: `ticket-system-release-branch`
5. **Add** butonuna tıkla

### 3c. Service Principal Oluştur ve Rol Ata

```bash
set -a && source .env && set +a

# Service principal oluştur (app registration'dan)
az ad sp create --id "$AZURE_CLIENT_ID"

# Scope: sadece bu kaynak grubuna katkı yetkisi
az role assignment create \
  --assignee "$AZURE_CLIENT_ID" \
  --role "Contributor" \
  --scope "/subscriptions/${AZURE_SUBSCRIPTION_ID}/resourceGroups/${RESOURCE_GROUP}"
```

---

## 4. GitHub Repository Ayarları

GitHub repo → **Settings** → **Secrets and variables** → **Actions** → **Variables** sekmesi
(Environment variables değil, **Repository variables** olacak):

| Variable Adı | Değer |
|---|---|
| `AZURE_CLIENT_ID` | `.env`'deki `AZURE_CLIENT_ID` |
| `AZURE_TENANT_ID` | `.env`'deki `AZURE_TENANT_ID` |
| `AZURE_SUBSCRIPTION_ID` | `.env`'deki `AZURE_SUBSCRIPTION_ID` |

---

## 5. İlk Deployment

Şimdi commit'leyip push'la:

```bash
git add -A
git commit -m "..."
git push origin release
```

GitHub Actions otomatik olarak deployment'ı başlatacak. `Actions` sekmesinden takip edebilirsin.

Uygulama: `https://${APP_NAME}.azurewebsites.net`

Demo hesaplar (seed data otomatik oluşur):
- **Admin**: admin@ticket.local / 123456
- **Destek**: destek@ticket.local / 123456
- **Müşteri**: musteri@ticket.local / 123456

---

## İzleme

```bash
set -a && source .env && set +a

# Logları canlı izle
az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP
```

## Sorun Giderme

- **502 Bad Gateway**: App Service'in PostgreSQL'e erişebildiğinden emin ol (firewall rule). `az postgres flexible-server firewall-rule list` ile kontrol et.
- **Redirect Loop**: Forwarded headers middleware'in doğru çalıştığından emin ol. App Service'te `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` environment variable'ını eklemeyi dene.
- **Migration hatası**: App Service loglarını kontrol et, connection string'in doğruluğunu doğrula.
