# V2’de eksik kalan teknik altyapı

Kaynak: `SanShine.Eba.Saas.Core`  
Karşılaştırılan: v2 altyapı özeti (`docs/saas-core-v2-altyapi-referans.md` + teslim checklist)

Amaç: kaynakta **var**, v2 özetinde **yok veya stub**, ama yeni çözümün sistem çekirdeği için **olması gereken** parçalar.

> **Güncel durum (2026-09-22):** P0 / P1 / P2 maddeleri `SanShine.Eba.Saas.CoreV2` içine taşındı.  
> Canlı özet: `SanShine.Eba.Saas.CoreV2/durum-ozet.md`  
> Bu dosya **neyin taşınması gerektiğinin** kaydı; “hâlâ eksik” listesi değil.

---

## Kapsam

| Taşınır | Taşınmaz |
|---|---|
| Auth / JWT / refresh / API kullanıcı tablosu | eBA SOAP, `EbaHelper`, WCF Connected Services |
| MFA (Otp.NET) | SAT, masraf, envanter, techsheet, e-imza, Superfilm, link-helper |
| DbManager senkron API + `DbManagerException` | EF `DbContext` / EF paketleri (kaynakta var, **kullanılmıyor**) |
| `BaseEntity`, JWT claim sözleşmesi, auth DTO/validator | `MoneyUtils` (masraf alanı) |
| `EbaIntegrationInfo` bind sınıfı (SOAP client değil) | `IUserService.GetAllEbaUsers` (eBA kullanıcı listesi) |
| `global.json`, solution klasör sözleşmesi | `IProcessService` / batch-approval |

---

## Öncelik özeti

| Öncelik | Ne | Neden |
|---|---|---|
| **P0** | Auth tam yığını (`api_users`, login, refresh, create-user) | v2 yalnız SuperAdmin stub; JWT pipeline’ın gerçek istemcisi yok |
| **P0** | JWT claim + refresh token üretimi | `[Authorize(Roles=...)]` ve token yenileme buna bağlı |
| **P0** | Auth DTO / validator / entity / query / repository | Feature checklist’in auth karşılığı; stub yetmez |
| **P1** | DbManager senkron metodlar + `DbManagerException` | Kaynak repository’ler sync kullanır; v2 yalnız async yazdı |
| **P1** | MFA (`IMfaService` + controller) | Paket (`Otp.NET`) v2 Application csproj’da var, kod yok |
| **P1** | `BaseEntity` + `BaseControllerResponse` factory’leri | Yeni entity/response kalıbı kaynakta bunlar |
| **P1** | `EbaIntegrationInfo` C# bind sınıfı | Config JSON kopyalandı, bind tipi yok |
| **P2** | `IEmailSendService` (MailKit) | Teknik altyapı; bugün yalnız e-imza tüketiyor |
| **P2** | `Utilities` ek metodları, `ResponseMessageType`, `IAppConfiguration` | Platform helper; v2 `GetConfiguration` ile yetindi |
| **P2** | `global.json` + solution `Source`/`Test` klasörleri | SDK kilidi ve solution grafı |

Hepsi V2’de **var**. Kapanmayan / bilinçli farklar:

| Konu | Not |
|---|---|
| `global.json` `rollForward` | V2 `latestMajor`, kaynak `latestMinor` |
| MailKit | V2 Application katmanında (kaynak Contract’taydı) |
| `DummyMailsModel` | Taşınmadı (e-imza P2) |
| Domain süreçleri | Hâlâ yok — sonraki iş |

---

## 1. Auth tam yığını (P0)

v2: `POST api/rest/auth/login2fa` SuperAdmin hash doğrula → token. Kullanıcı tablosu ve refresh **bilinçli stub**.

Kaynakta bu **sistem altyapısıdır** (süreç değil). REST istemcileri `api_users` üzerinden yaşar.

### 1.1 Endpoint’ler

| Method | Path | Auth | Davranış |
|---|---|---|---|
| POST | `/api/rest/auth/login2fa` | açık | SuperAdmin **veya** `api_users` + hash + rol + refresh kaydı |
| POST | `/api/rest/auth/login-refresh` | açık | Refresh doğrula → yeni access + yeni refresh |
| POST | `/api/rest/auth/create-user` | `[Authorize(Roles="SuperAdmin")]` | PBKDF2 hash + `INSERT api_users` |

Kaynak tuhaflıkları (kopyala, düzeltme):

- `login2fa` ve `create-user` **`HandleServiceResponse` kullanmaz**.
- `create-user` dönüş tipi `IActionResult` değil, `CreateRestApiUserResponse` (ASP.NET default 200).
- SuperAdmin yolunda refresh token **üretilmez / kaydedilmez**.
- Config `SuperAdminUser.Role = "Super_Admin"`; JWT claim `RoleType.SuperAdmin` → `"SuperAdmin"`. `[Authorize(Roles="SuperAdmin")]` JWT claim’e bakar.

### 1.2 Katman dosyaları

```
Application/Services/IAuthService.cs          ← Login2FA + LoginRefresh + CreateRestApiUser
Infrastucture/Repositories/IAuthenticationRepository.cs
Infrastucture/Queries/AuthenticationQueries.cs
Infrastucture/Entities/RestApiUserEntity.cs
Infrastucture/Entities/RefreshTokenEntity.cs
Infrastucture/Entities/UserRoleEntity.cs
Infrastucture/Entities/CreateRestApiEntity.cs
Infrastucture/Entities/RestApiUserInfoEntity.cs
Contract/Requests/Auth/RestApiUserLoginRequest.cs
Contract/Requests/Auth/RefreshTokenRequest.cs
Contract/Requests/Auth/CreateRestApiUserRequest.cs
Contract/Responses/Auth/RestApiUserLoginResponse.cs
Contract/Responses/Auth/LoginResponse.cs
Contract/Responses/Auth/SaveRefreshTokenResponse.cs   ← içinde LoginRefreshResponse
Contract/Responses/Auth/CreateRestApiUserResponse.cs
Contract/Responses/Auth/RestApiUserResponse.cs
Contract/Validator/LoginAuthValidator.cs
Contract/Validator/RefreshLoginValidator.cs
Contract/Validator/CreateApiUserValidator.cs
```

### 1.3 `api_users` sözleşmesi

```sql
-- AuthenticationQueries (kaynak)
INSERT api_users (username, password, status, company_id, company, branch_id, branch, role, responsible_user_name, created_at, created_by)
SELECT id, username, password FROM api_users WHERE username = @username AND status = 'A'
UPDATE api_users SET refresh_token = @refresh_token WHERE username = @username
SELECT role FROM api_users WHERE username = @username
SELECT id, username, role, refresh_token, responsible_user_name FROM api_users WHERE refresh_token = @refresh_token
```

Username create’te `Guid.NewGuid()` ile üretilir (istekten gelmez).

### 1.4 DI

v2 `ApplicationModule`: `IPasswordHashService`, `IAuthService`, `IHealthService`  
v2 `DataAccessModule`: `IHealthRepository`

Eksik kayıtlar:

```csharp
services.AddTransient<IAuthenticationRepository, AuthenticationRepository>();
```

(`IAuthService` Autofac taramasıyla da gelir; kaynak her iki listeyi de tutar.)

### 1.5 Validator kuralları

| Validator | Kural |
|---|---|
| `LoginAuthValidator` | Username NotEmpty; Password NotEmpty + MinLength 8 |
| `RefreshLoginValidator` | RefreshToken NotEmpty |
| `CreateApiUserValidator` | Password MinLength 8 + complexity regex; Company NotEmpty |

---

## 2. JWT claim sözleşmesi ve refresh üretimi (P0)

v2: `Jwt.GetJwt(...)` + `JwtRestApi` sabitleri.

Kaynakta eksik kalan API:

```csharp
// Application/Shared/Jwt.cs
GetJwt(JwtGenerationUserModel user, out loginToken, out refreshToken)
GetJwt(int userId, string username, out loginToken, out refreshToken, string userRole = "Member")
GenerateRefreshToken()   // 32 byte RNG → Base64; JWT değil
```

Claim sabitleri (`Contract/Shared/JwtTokenClaims.cs`):

| Claim | Değer |
|---|---|
| `user_id` | kullanıcı id |
| `username` | kullanıcı adı *(int overload’da yanlışlıkla `userId.ToString()` yazılır — kaynak davranışı)* |
| `ClaimTypes.Role` | rol string |
| `responsible_user` | yalnız `JwtGenerationUserModel` overload’ında |

Modeller:

- `Contract/Modules/JwtGenerationUserModel.cs` — `UserId`, `UserName`, `Role` (default `Member`), `ResponsibleUserName`
- `LoginResponse` — `AccessToken`, `RefreshToken`, `AuthHelper` (`Bearer {jwt}`), `Username`, `CreatedAt`, `ExpireDate`

Refresh access JWT’den ayrıdır: login sonrası `GenerateRefreshToken()` + `SaveRefreshToken` (`api_users.refresh_token`).

---

## 3. MFA (P1)

v2 Application paketi: `Otp.NET 1.4.0` — servis, controller, DI satırı **yok**.

Kaynak:

```
Application/Services/IMfaService.cs
WebApi/Controllers/MfaController.cs    ← ControllerBase değil, Controller
```

| Method | Path | Girdi | Çıktı |
|---|---|---|---|
| POST | `/api/rest/mfa/get-secret-key` | — | `{ SecretKey }` Base32 |
| POST | `/api/rest/mfa/generate-totp` | `secretKey`, `timeStep=30` | `{ SecretKey, TimeStep, Otp }` |
| POST | `/api/rest/mfa/validate-totp` | `secretKey`, `code`, `timeStep` | `{ SecretKey, Code, Validate }` |

Davranış:

- Secret: 20 byte random → Base32
- Default time step sınıfta `90` (yorum “30 saniye” der; kaynak)
- Validate: `VerificationWindow(previous: 1, future: 1)`
- `HandleServiceResponse` / `BaseControllerResponse` yok
- `[Authorize]` yok (açık endpoint)

DI:

```csharp
services.AddTransient<IMfaService, MfaService>();
```

---

## 4. DbManager — v2’nin kestiği yüzey (P1)

v2 yalnız 4 metod yazdı: `ReadAsync`, `ExecuteNonQueryAsync`, `ExecuteNonQueryTransactionAsync`, `RunProcedureAsync`.

Kaynakta ayrıca:

| Metod | Anlam |
|---|---|
| `Read<T>` | Sync SELECT → `List<T>` |
| `ExecuteNonQuery` | Sync; `rowsAffected > 0` |
| `RunProcedure` | Sync SP; `rowsAffected == -1` → true |
| `DbManagerException` | Nested type: `ErrorCode`, `ProcedureName`; SP hatalarında fırlatılır |

Auth repository sync kullanır:

- `GetRestApiUserByUsername` → `DbManager.Read`
- `SaveRefreshToken` → `DbManager.ExecuteNonQuery`
- `GetUserRoleByUsername` → `DbManager.Read`

Sync kopyalanmazsa auth yığını derlenmez veya async’e çekilmiş olur (kaynak davranış bozulur).

`sColumn`, `AddWithValue`, TX’de yalnız 1 satır commit — v2’de doğru; dokunma.

---

## 5. Entity / response kalıpları (P1)

### 5.1 `BaseEntity`

```
Infrastucture/Entities/BaseEntities/BaseEntity.cs
```

`sColumn`: `created_at`, `created_by`, `updated_at`, `updated_by`.  
`CreateRestApiEntity` ve `RestApiUserInfoEntity` bundan türer. Yeni tablo entity’leri aynı kalıbı kullanır.

### 5.2 `BaseControllerResponse` factory’leri

v2 yalnız `SetSuccess` / `SetFailure` (instance) yazdı.

Kaynakta ek:

```csharp
SetNoContent<T>(...)
SetNotFound<T>(...)
SetFailure<T>(message, status)
SetSuccess<T>(message, status)      // “Just Use Transaction Result”
SetUnprocessableContent<T>(...)
```

Hepsi `new T { StatusCode, Message }` döner. Service’ler bu overloads’ı kullanır.

### 5.3 İkinci response ailesi (opsiyonel / ölü)

```
Contract/Responses/BaseResponses/BaseResponse.cs      // Success, Message, audit alanları; Newtonsoft
Contract/Responses/BaseResponses/ValidationResult.cs  // Success + Message; EbaHelper dönüş tipi
```

`CustomBaseController` bunları kullanmaz. `ValidationResult` SOAP/EbaHelper’a bağlı → v2’ye **alma**. `BaseResponse` kaynakta neredeyse kullanılmıyor; zorunlu değil.

---

## 6. Config bind ve sabitler (P1 / P2)

### 6.1 `EbaIntegrationInfo` sınıfı (P1)

v2 JSON’da section var; SOAP client yok. Bind tipi de yok:

```csharp
// Common/Constants/EbaIntegrationInfo.cs
UserId, Password, Language, Domain, IsDevops
```

SOAP yazılmasa bile `IConfiguration.GetSection("EbaIntegrationInfo").Get<EbaIntegrationInfo>()` için sınıf durmalı. Aksi halde config ölü kalır.

### 6.2 `ResponseMessageType` (P2)

```
Application/Enum/ResponseMessageType.cs
Success | Failed | UserNotFound | UserRoleNotFound | AuthFailed
```

Auth refresh/role-not-found mesajları enum `.ToString()` ile gider (`"UserNotFound"`). `ResponseMessages` (Türkçe süreç mesajları) domain’e yakın; auth için enum yeter.

### 6.3 `IAppConfiguration` (P2)

```
Contract/Modules/IAppConfiguration.cs
Audience { Secret, Iss, Aud } + TokenExpiration + RefreshTokenExpiration
```

`Program.cs` bunu **bind etmez**; JWT `JwtRestApi` sabitlerinden okur. Kaynakta duran iskelet; ileride config’e çekmek için kopyalanabilir. Çalışan auth için zorunlu değil.

### 6.4 `SuperAdminCredentials.Token`

v2 sınıfı kopyalandıysa `Token` property’sini de al. Kaynakta var, login set etmez.

### 6.5 `LogConstants`

v2: `FolderName`, `CommonFileName`.  
Kaynak ekstra: `TechSheetFileName`, `CuzdanFileName` → **domain, alma**.

---

## 7. `Utilities` ek metodları (P2)

v2: `GetConfiguration` + (LogHelper için) `GetTimeFormat`.

Kaynak ekstra:

| Metod | Ne işe yarar |
|---|---|
| `ToCamelCase` | `tr-TR` ToLower + ToTitleCase |
| `NormalizeToEnglish` | ÇĞİÖŞÜ → ASCII |
| `AddCountryCode` | 10/11/12 hane telefona `+90` |

Bugün `IUserService` / masraf validator kullanır. Platform helper; yeni feature’lar (kullanıcı/telefon/isim) için çekirdekte durmalı. Masraf özel `MoneyUtils` **alma**.

---

## 8. E-posta altyapısı (P2)

```
Application/Services/IEmailSendService.cs
Paket: MailKit 4.10.0   ← kaynakta Contract csproj’da (yanlış katman; v2’de Application’a koy)
```

```csharp
Task<bool> SendEmailAsync(to, subject, htmlTemplate, cc, bcc, attachments = null)
```

Sabitler (kaynak): `MAILFROM = dys@sanko.com.tr`, SMTP `appposta.sanko.com.tr:25`, `StartTls`, auth boş.

- To/Cc/Bcc `;` veya `,` ile split
- Hata: `LogHelper` → klasör `MailErrorLog`, swallow, `false`
- `ApplicationModule`’da **yok**; yalnız Autofac assembly tarama register eder
- Tek tüketen: `ESignatureService` (domain)

`DummyMailsModel` (`EBA@EBA.COM` vb. HashSet) yalnız e-imza mail elemesinde kullanılır → **P2, e-imza gelene kadar opsiyonel**.

---

## 9. Host / solution iskeleti (P2)

Kaynakta v2 özetinde yazılmayanlar:

| Dosya | İçerik |
|---|---|
| `global.json` | SDK `6.0.0`, `rollForward: latestMinor`, `allowPrerelease: false` |
| `Properties/launchSettings.json` | `https://localhost:7191;http://localhost:5284`, `launchUrl: swagger`, IIS Express profili |
| `appsettings*.json` | `Logging.LogLevel`, `AllowedHosts: *` (v2 örneği yalnız ConnectionStrings + SuperAdmin) |
| `.sln` solution folders | `Source`, `Test`, `Build`, `SolutionItems` |
| `SanShine.SaaS.Core.Test` | Boş console (`Hello, Sanko.!`) — test altyapısı değil; kopyalamak zorunlu değil |

CORS / Newtonsoft MVC / FluentValidation auto-register / `IHttpContextAccessor` / health-check **kaynakta da yok**. Pipeline v2 sırası doğru.

---

## 10. Controller / AOP davranış notları (kopyala)

v2 checklist “her controller `CustomBaseController` + `HandleServiceResponse`” der. Kaynak istisnaları:

| Controller | Taban | Not |
|---|---|---|
| `AuthController.login2fa` | `CustomBaseController` | Elle `Unauthorized` / `Ok`; `isAuthorize` flag |
| `AuthController.create-user` | `CustomBaseController` | `IActionResult` değil |
| `MfaController` | `Controller` | Anonim JSON |
| `TestController` | `Controller` | Environment string |

`[ValidationAspect]` implementation metodunda (interface’de değil) — v2 doğru.

`DatabaseExceptionHandlingAspect` register edilir, attribute olarak **kullanılmaz** — v2 doğru.

---

## 11. Yeni feature checklist’e eklenecek Auth satırları

v2 checklist Health örneği ile bitiyor. Auth için tam zincir:

1. Request: `RestApiUserLoginRequest` / `RefreshTokenRequest` / `CreateRestApiUserRequest`
2. Response: `RestApiUserLoginResponse` / `LoginRefreshResponse` / `CreateRestApiUserResponse` (`BaseControllerResponse`)
3. Validator: `LoginAuthValidator` / `RefreshLoginValidator` / `CreateApiUserValidator`
4. Queries: `AuthenticationQueries` (`api_users`)
5. Entity + `sColumn` (`RestApiUserEntity`, `RefreshTokenEntity`, `UserRoleEntity`, `CreateRestApiEntity : BaseEntity`)
6. `IAuthenticationRepository` — boş ctor, sync+async `DbManager`
7. `IAuthService` — `[ValidationAspect]`, SuperAdmin config, `IPasswordHashService`, `Jwt.GetJwt` / `GenerateRefreshToken`
8. Transient: Application + DataAccess
9. `AuthController` — `login2fa` açık, `create-user` SuperAdmin role

---

## Taşınmayacaklar (kaynakta var, v2’ye alma)

Bunlar teknik görünümlü ama domain veya ölü paket:

- `EbaHelper` + `eBAServiceReference` / `EbaNetServiceReference` + `System.ServiceModel.*`
- `Microsoft.EntityFrameworkCore*` (Contract’ta paket var, `DbContext` yok)
- Süreç controller/service/query/entity (SAT, DSA, masraf, envanter, techsheet, e-imza, Superfilm, link-helper)
- `IUserService` / `UsersController` (`get-all-eba-users-for-mobile`)
- `IProcessService` / `batch-approval`
- `MoneyUtils`
- `LogConstants.TechSheetFileName` / `CuzdanFileName`
- `ResponseMessages` süreç metinleri (`ProcessNotStarted`, …) — auth enum ayrı

---

## Minimum ek teslim (v2’ye uygulanacak sıra)

1. `JwtTokenClaims` + `JwtGenerationUserModel` + `Jwt.GenerateRefreshToken` + ikinci `GetJwt` overload  
2. `BaseEntity` + `BaseControllerResponse` generic factory’ler  
3. DbManager: `Read`, `ExecuteNonQuery`, `RunProcedure`, `DbManagerException`  
4. Auth DTO → validator → queries → entity → repository → service → controller → Transient satırları  
5. `EbaIntegrationInfo` bind class  
6. `IMfaService` + `MfaController` + Transient  
7. `global.json` + `Logging`/`AllowedHosts` appsettings  
8. (opsiyonel) `IEmailSendService` + MailKit Application katmanında
