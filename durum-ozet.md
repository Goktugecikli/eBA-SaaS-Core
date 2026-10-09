# V2 durum özeti

Tarih: 2026-09-22  
Solution: `SanShine.SaaS.Core.V2.sln` · **net6.0** · derleme: **0 hata**

Kapsam: **sistem altyapısı**. eBA süreçleri (SOAP, SAT, masraf, envanter, techsheet, e-imza) **yok**.

---

## Ne bitti

İki adım uygulandı:

1. `saas-core-v2-altyapi-referans.md` → 5 katman iskeleti, AOP, middleware, DbManager, Health örneği, JWT stub  
2. `v2-eksik-teknik-altyapi.md` → Auth tam yığın, MFA, BaseEntity, mail, host iskeleti

Kaynak kalıp: `SanShine.Eba.Saas.Core` (domain kopyalanmadı).

---

## Katmanlar

```
SanShine.SaaS.Core.V2.sln
  Source
    SanShine.Saas.Core.WebApi
    SanShine.SaaS.Core.Application
    SanShine.SaaS.Core.Contract
    SanShine.SaaS.Core.Infrastucture   ← typo korundu
    SanShine.SaaS.Core.Common
  Test / Build / SolutionItems        ← klasör var, Test projesi yok
```

| WebApi → Application, Contract  
| Application → Common, Infrastucture  
| Infrastucture → Common, Contract  
| Contract → Common |

---

## Teknik çekirdek (çalışıyor)

| Parça | Durum |
|---|---|
| Autofac SingleInstance + Castle interceptor | var |
| MS DI Transient (Application + DataAccess) | var |
| `ValidationAspect` + FluentValidation | var |
| `ExceptionMiddleware` (404/422/400/500) | var |
| `LogHelper` dosya log | var |
| `IResult` / `BusinessRules` | var |
| `CustomBaseController` + `BaseControllerResponse` factory | var |
| `DbManager` async + **sync** + `DbManagerException` + TX 1 satır | var |
| `Utilities.GetConfiguration` (merge yok) | var |
| JWT Bearer (`JwtRestApi`, ClockSkew=0) | var |
| Swagger her ortam | var |
| `global.json` (`6.0.0`, `rollForward: latestMajor`) | var |
| `appsettings` / `appsettings.Development` (asıl projeden) | var |

---

## Auth (P0 — artık stub değil)

`api_users` üzerinden tam zincir:

| Method | Path | Auth |
|:------:|---|---|
| POST | `/api/rest/auth/login2fa` | açık — SuperAdmin **veya** `api_users` + refresh kaydı |
| POST | `/api/rest/auth/login-refresh` | açık |
| POST | `/api/rest/auth/create-user` | `[Authorize(Roles="SuperAdmin")]` |

Kaynak tuhaflıkları korundu: `login2fa`/`create-user` `HandleServiceResponse` kullanmaz; SuperAdmin JWT claim `"SuperAdmin"` (config Role `"Super_Admin"` değil).

---

## Diğer açık endpoint’ler

| Method | Path | Not |
|:------:|---|---|
| POST | `/api/rest/health-echo` | echo + `SELECT 1` |
| GET | `/api/rest/SKY/get-environment` | `ASPNETCORE_ENVIRONMENT` |
| POST | `/api/rest/mfa/get-secret-key` | açık |
| POST | `/api/rest/mfa/generate-totp` | açık |
| POST | `/api/rest/mfa/validate-totp` | açık |

---

## DI kayıtları (MS Transient)

**ApplicationModule:** `IPasswordHashService`, `IAuthService`, `IHealthService`, `IMfaService`  
**DataAccessModule:** `IHealthRepository`, `IAuthenticationRepository`  
**Autofac tarama:** Application assembly (AOP) + `IEmailSendService` (MS listesinde yok)

---

## Config

| Ortam | SQL | SOAP host (yalnız JSON) |
|---|---|---|
| Production | `SNKSQLFNS` / `EBA` | `eba.sanko.com.tr` |
| Development | `EBATEST` | `ebatest.sanko.com.tr` |

`EbaIntegrationInfo` **bind sınıfı var**, SOAP client **yok**.

---

## P0 / P1 / P2 kapanış (`v2-eksik-teknik-altyapi.md`)

| Öncelik | Parça | V2 |
|---|---|---|
| P0 | Auth tam yığın (`api_users`, login, refresh, create-user) | taşındı |
| P0 | JWT claim + `GenerateRefreshToken` + `JwtGenerationUserModel` | taşındı |
| P0 | Auth DTO / validator / entity / query / repository | taşındı |
| P1 | DbManager sync + `DbManagerException` | taşındı |
| P1 | MFA (`IMfaService` + controller + Transient) | taşındı |
| P1 | `BaseEntity` + `BaseControllerResponse` factory | taşındı |
| P1 | `EbaIntegrationInfo` bind sınıfı | taşındı |
| P2 | `IEmailSendService` + MailKit | taşındı (Application katmanı) |
| P2 | `Utilities` ek metodlar, `ResponseMessageType`, `IAppConfiguration` | taşındı |
| P2 | `global.json` + solution `Source`/`Test`/`Build` klasörleri | taşındı (Test **projesi** yok) |

Sistem altyapısı kapanmış. Sonraki sprint domain süreçleri.

---

## İnceleme notları (kaynak vs V2)

Kodla doğrulandı. Özetin ana iddiaları doğru; sapmalar:

| Konu | Durum |
|---|---|
| `global.json` `rollForward` | V2: `latestMajor`. Kaynak: `latestMinor`. SDK 7+’a kayabilir. |
| MailKit katmanı | Kaynak Contract’taydı; V2 **Application**’a alındı (doğru). |
| `Microsoft.Extensions.Configuration.Binder` | Kaynak Contract’ta yoktu; SuperAdmin `Get<T>()` için V2’de var (gerekli). |
| `Utilities.ToCamelCase` / `NormalizeToEnglish` / `AddCountryCode` | Kodda var; önceki özet yazmamıştı. |
| `IAppConfiguration` | Kodda var; `Program.cs` bind etmez (kaynak da etmez). JWT `JwtRestApi` sabitleri. |
| `DummyMailsModel` | Yok. P2’ydi; e-imza gelene kadar doğru. |
| `LogConstants` TechSheet/Cuzdan | Yok. Domain, alınmadı. |
| `MoneyUtils` | Yok. Masraf alanı, alınmadı. |
| Test projesi | Klasör var, proje yok. Kopyalamamak doğruydu. |

Kaynak tuhaflıkları korundu: `login2fa` username claim’inde `userId.ToString()` (int overload); SuperAdmin refresh üretmez.

---

## Bilinçli olarak yok (sonraki iş)

- `EbaHelper` / WCF / `eBAServiceReference`
- SAT, dış SAT, masraf, envanter, techsheet, e-imza, Superfilm, link-helper
- `IUserService` / `IProcessService` / batch-approval
- EF, MediatR, generic repository, Serilog
- Test projesi (`Hello, Sanko.!`)

---

## Çalıştırma

```text
dotnet run --project SanShine.Saas.Core.WebApi
```

Swagger: `http://localhost:5284/swagger` · HTTPS: `https://localhost:7191/swagger`
