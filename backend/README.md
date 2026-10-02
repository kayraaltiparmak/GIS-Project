# Yerel geliştirme yapılandırması

`appsettings.json` paylaşılabilir ayarları içerir. `ConnectionStrings:DefaultConnection`
ve `Jwt:Key` bilinçli olarak boş bırakılmıştır; gerçek değerleri bu dosyaya yazmayın.

Proje kökünden yerel değerleri ASP.NET Core User Secrets deposuna kaydedin:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=YOUR_DATABASE;Username=YOUR_USER;Password=YOUR_PASSWORD" --project backend/backend.csproj
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SIGNING_KEY_AT_LEAST_32_BYTES" --project backend/backend.csproj
dotnet build backend/backend.csproj
dotnet run --project backend/backend.csproj --launch-profile http
```

Yer tutucuları kendi değerlerinizle değiştirin. User Secrets kimliği projede tanımlıdır;
yeniden `init` çalıştırmak gerekmez. `http` profili Development ortamını kullanır ve
`WebApplication.CreateBuilder` bu ortamda User Secrets değerlerini otomatik yükler.
Windows deposu `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
konumundadır; repository dışında tutulur fakat şifrelenmez. Gerçek değerler içeren
komutları paylaşılan terminale veya kaydedilen shell geçmişine yazmayın.

Diğer ortamlarda secret yöneticiniz aracılığıyla
`ConnectionStrings__DefaultConnection` ve `Jwt__Key` environment variables
sağlayabilirsiniz. Environment variables User Secrets ve JSON ayarlarını geçersiz kılar.
Production ortamı User Secrets deposunu otomatik yüklemez.

Önceden commit edilmiş secret'ları config dosyasından kaldırmak Git geçmişini
temizlemez. PostgreSQL parolasını ve JWT imzalama anahtarını yenileyin, yerel secret
deposunu güncelleyin. JWT anahtarını yenilemek mevcut token'ları geçersiz kılar.
Paylaşımdan önce eski secret'ları Git geçmişinden de temizleyin; daha önce push
edildiyse geçmişin yeniden yazılmasını diğer katkıcılarla koordine edin.
