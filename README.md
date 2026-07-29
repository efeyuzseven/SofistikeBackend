# SofistikeBackend

Sofistike +XTRA'nın B2C, B2B ve entegrasyon ihtiyaçları için başlangıç Web API
yapısıdır. Bu repo yalnızca temel mimariyi içerir; iş modülleri ekip tarafından
geliştirilecektir.

## Teknoloji

- .NET 10 Web API
- Controller tabanlı HTTP API
- OpenAPI
- xUnit

## Yerel geliştirme

Gereksinim: .NET SDK `10.0.302` veya uyumlu daha yeni bir feature band.

```bash
dotnet restore
dotnet run --project src/Sofistike.Api
```

API varsayılan olarak `http://localhost:5118` adresinde çalışır.

```http
GET /api/v1/system/health
```

## Komutlar

```bash
dotnet build Sofistike.slnx
dotnet test Sofistike.slnx
dotnet format Sofistike.slnx --verify-no-changes
```

## Katmanlar

```text
src/
├── Sofistike.Api             # HTTP, middleware ve uygulama başlangıcı
├── Sofistike.Application     # use-case ve uygulama sözleşmeleri
├── Sofistike.Domain          # saf iş kuralları ve domain modeli
└── Sofistike.Infrastructure  # veri erişimi ve dış servis adaptörleri
tests/
├── Sofistike.UnitTests
└── Sofistike.IntegrationTests
```

Bağımlılık yönü:

```text
Api -> Application
Api -> Infrastructure
Infrastructure -> Application -> Domain
```

Domain katmanı başka bir proje katmanını referanslamamalıdır. Veri tabanı,
ödeme, kargo, pazaryeri ve mesajlaşma sağlayıcılarına ait kodlar
Infrastructure katmanında tutulmalıdır.

## Başlangıç kapsamı

Şimdilik yalnızca proje/katman yapısı, hata yanıtı altyapısı, CORS, OpenAPI,
health endpoint'i, örnek testler ve CI hazırlanmıştır. Ürün, sipariş, sepet,
stok, kampanya, müşteri, B2B ve entegrasyon modelleri bilinçli olarak
eklenmemiştir.

## Git akışı

- Çalışmalar kısa ömürlü feature branch'lerde yapılır.
- `main` ve `develop` branch'lerine pull request üzerinden gidilir.
- Pull request öncesinde build, test ve format kontrolleri çalıştırılır.
- Commitler küçük, açıklayıcı ve tek bir değişiklik odağında tutulur.
