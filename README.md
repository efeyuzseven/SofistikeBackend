# SofistikeBackend

Sofistike +XTRA'nın B2C, B2B ve entegrasyon ihtiyaçları için başlangıç Web API
yapısıdır. Bu repo yalnızca temel mimariyi içerir; iş modülleri ekip tarafından
geliştirilecektir.

## Teknoloji

- .NET 10 Web API
- Controller tabanlı HTTP API
- Entity Framework Core 10
- SQL Server
- OpenAPI
- xUnit

## Yerel geliştirme

Gereksinim: .NET SDK `10.0.302` veya uyumlu daha yeni bir feature band.

```bash
dotnet restore
dotnet run --project src/Sofistike.Api
```

API varsayılan olarak `http://localhost:5118` adresinde çalışır.

Yerel veritabanı, SQL Server ailesindeki `MSSQLLocalDB` örneğinde Windows kimlik
doğrulamasıyla `SofistikeDb` adıyla çalışır. API geliştirme ortamında açılırken
LocalDB'yi migrate eder ve örnek hesap/katalog verilerini hazırlar. LocalDB durmuşsa
önce şu komutu çalıştırın:

```powershell
SqlLocalDB start MSSQLLocalDB
```

Migration'ları API'yi açmadan elle uygulamak için:

```bash
dotnet ef database update \
  --project src/Sofistike.Infrastructure \
  --startup-project src/Sofistike.Api
```

```http
GET /api/v1/system/health
```

### Yerel giriş hesapları

Yalnızca geliştirme ortamında otomatik hazırlanan hesaplar:

- Yönetici: `admin@sofistike.com` / `Admin123!`
- Müşteri: `umay@sofistike.com` / `Umay123!`

Kimlik doğrulama uçları:

```http
POST /api/v1/auth/login
POST /api/v1/auth/register
GET  /api/v1/auth/me
POST /api/v1/auth/logout
GET  /api/v1/account/profile
PUT  /api/v1/account/profile
GET  /api/v1/account/favorites
POST /api/v1/account/favorites/{productId}
DELETE /api/v1/account/favorites/{productId}
GET  /api/v1/account/cart
POST /api/v1/account/cart/items
PATCH /api/v1/account/cart/items/{productId}
DELETE /api/v1/account/cart/items/{productId}
POST /api/v1/account/orders
GET  /api/v1/account/orders
POST /api/v1/account/orders/{orderId}/cancel
GET  /api/v1/account/reviews
POST /api/v1/account/reviews
```

Katalog uçları:

```http
GET /api/v1/categories
GET /api/v1/products
GET /api/v1/products/{slug}
POST /api/v1/admin/products
PUT  /api/v1/admin/products/{productId}
DELETE /api/v1/admin/products/{productId}
```

Ürün listesi `category`, `search`, `isPopular`, `isXtra`, `inStock`,
`minPrice`, `maxPrice`, `sort`, `page` ve `pageSize` sorgu parametrelerini
destekler. Geliştirme ortamında migration uygulandıktan sonra, katalog boşsa
örnek ürünler otomatik olarak eklenir. Bu geliştirme verileri canlı ortamda
oluşturulmaz.

Bu hesap yalnızca yerel arayüz ve entegrasyon geliştirmesi içindir. Üretimde
geliştirme hesabı seed edilmemeli ve kullanıcı parolaları uygulamanın üretim
kimlik yönetimi politikalarına göre oluşturulmalıdır.

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

Şu anda kimlik doğrulama, kullanıcı profili, kullanıcı favorileri, kalıcı sepet,
sipariş oluşturma/geçmiş/iptal, ürün değerlendirmeleri, public ürün kataloğu ve
yönetici ürün ekleme akışı hazırlanmıştır. Katalog; kategori, ürün, varyant,
görsel, fiyat, kampanyalı fiyat, depo ve stok modellerini içerir.
Siparişler ödeme sağlayıcısı bağlanana kadar `AwaitingPayment` / `Pending`
durumuyla oluşturulur. Ham kart numarası ve CVV API'ye alınmaz veya saklanmaz.
`DELETE` ürünü fiziksel olarak silmez; geçmiş siparişleri koruyarak arşivler ve
mağaza kataloğundan kaldırır. Kategori yönetimi, değerlendirme moderasyonu,
gerçek ödeme, B2B ve pazaryeri entegrasyonları sonraki geliştirme aşamalarındadır.

## Git akışı

- Çalışmalar kısa ömürlü feature branch'lerde yapılır.
- `main` ve `develop` branch'lerine pull request üzerinden gidilir.
- Pull request öncesinde build, test ve format kontrolleri çalıştırılır.
- Commitler küçük, açıklayıcı ve tek bir değişiklik odağında tutulur.
