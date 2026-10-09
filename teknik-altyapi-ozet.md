# SanShine.SaaS.Core.V2 — Teknik Altyapı Özeti

Hedef: **yalnız sistem altyapısı**. Domain süreçleri (eBA SOAP, SAT, masraf, envanter, techsheet, e-imza) **yok**.

Kaynak kalıp: `SanShine.Eba.Saas.Core`  
Çalıştırma: `dotnet run --project SanShine.Saas.Core.WebApi` → Swagger `http://localhost:5284/swagger` (HTTPS: `7191`)

---

## 1. Solution grafı

| Proje | SDK | Görev |
|---|---|---|
| `SanShine.Saas.Core.WebApi` | Web | Controller, JWT pipeline, Swagger, MS DI kayıtları |
| `SanShine.SaaS.Core.Application` | Classlib | Servis, Autofac AOP, JWT üretimi, password hash |
| `SanShine.SaaS.Core.Contract` | Classlib | Request / Response / Validator / JWT modelleri |
| `SanShine.SaaS.Core.Infrastucture` | Classlib | ADO.NET `DbManager`, entity, query, repository *(typo bilinçli)* |
| `SanShine.SaaS.Core.Common` | Classlib | Aspect, middleware, log, result, exception, config okuma |

```
WebApi        → Application, Contract
Application   → Common, Infrastucture
Infrastucture → Common, Contract
Contract      → Common
Common        → yok
```

Hepsi **net6.0**, `ImplicitUsings`, `Nullable`. EF / MediatR / generic repository **yok**.

---

## 2. Pipeline (`Program.cs` sırası — değişmez)

1. `AddControllers` + Swagger + Bearer security definition  
2. Autofac host (`AutofacApplicationModule`)  
3. `AddApplicationServices()` (MS DI Transient)  
4. `AddDataAccess()` (MS DI Transient)  
5. JWT Bearer (`JwtRestApi` sabitleri, `ClockSkew = Zero`)  
6. `AddAuthorization`  
7. `Build`  
8. `ConfigureCustomExceptionMiddleware` — pipeline’ın başı  
9. Swagger her ortamda açık  
10. `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapControllers`

---

## 3. Çift DI

**A — Autofac (AOP bununla çalışır)**  
Application assembly taranır → `AsImplementedInterfaces` + Castle DynamicProxy + `AspectInterceptorSelector` → **SingleInstance**.  
Ayrıca `ValidationAspect` ve `DatabaseExceptionHandlingAspect` register (ikincisi attribute olarak kullanılmaz).

**B — MS DI Transient (elle liste)**

| Modül | Kayıt |
|---|---|
| `ApplicationModule` | `IPasswordHashService`, `IAuthService`, `IHealthService` |
| `DataAccessModule` | `IHealthRepository` |

Autofac host olduğu için **A kazanır**; B kaynak davranışını korur.

---

## 4. AOP / Validasyon

```
MethodInterceptionBaseAttribute (IInterceptor)
  └── MethodInterception   OnBefore → Proceed → OnException/OnSuccess → OnAfter
        └── ValidationAspect
```

- Validator tipi `IValidator` değilse: `"Bu bir doğrulama sınıfı değil"`
- `OnBefore`: request tipi eşleşen argüman → `ValidationTool.Validate`
- Fail → `FluentValidation.ValidationException` → middleware **422**

Attribute **implementation metodunda** durur. Örnek: `[ValidationAspect(typeof(HealthValidator))]`

---

## 5. Exception middleware

| Tip | HTTP | Body |
|---|---|---|
| `NotFoundException` | 404 | `ErrorDetails` + ErrorId (erken return, log yok) |
| `ValidationException` | 422 | `ValidationErrorDetails` + Errors |
| `BusinessRulesException` | 400 | `ErrorDetails` |
| `DatabaseException` | 500 | genel DB mesajı |
| diğer | 500 | `Internal Server Error.` |

JSON: Newtonsoft. Log klasörü: `Logs/{Yıl}/{Ay}/SystemErrors/{path}`.  
Servisler çoğunlukla **throw etmez** → `SetFailure` + `StatusCode`.

---

## 6. Result + BusinessRules

`IResult` / `Result` / `SuccessResult` / `ErrorResult`  
`IDataResult<T>` / `DataResult<T>` / `SuccessDataResult<T>` / `ErrorDataResult<T>`

`BusinessRules.Run(params IResult[])`: ilk `Success == false` döner, hepsi geçerse `null`.

---

## 7. Controller + cevap sözleşmesi

- Route kökü: `[Route("api/rest/")]`
- Controller `CustomBaseController` türetir
- Cevap: `BaseControllerResponse` (`Message`, `[JsonIgnore] StatusCode`, `SetSuccess` / `SetFailure`)
- `HandleServiceResponse<T>`: OK / Created / NoContent / BadRequest / Unauthorized / Forbid / NotFound / Conflict

---

## 8. Log

`LogHelper.WriteToFileAsync` — Serilog / `ILogger` **yok**.  
Yol: `{cwd}/Logs/{Year}/{Month}/{folder}/{dd.MM.yyyy}_{file}.txt`  
`LogConstants.FolderName = "Api Çalışma Logs"`

---

## 9. Veri erişimi (`DbManager`)

Static ADO.NET (`Microsoft.Data.SqlClient`). Connection: `Utilities.GetConfiguration().GetConnectionString("DefaultConnection")`.

| Metod | Anlam |
|---|---|
| `ReadAsync<T>` | SELECT → `List<T>`, yalnız `[DbManager.sColumn("alias")]` map |
| `ExecuteNonQueryAsync` | `rowsAffected > 0` |
| `ExecuteNonQueryTransactionAsync` | TX; **yalnız 1 satır** commit |
| `RunProcedureAsync` | SP; `rowsAffected == -1` → true |

Repository ctor **boş**. SQL: `internal static class XxxQueries`.

---

## 10. Config

`Utilities.GetConfiguration()` **merge yok**:

- `ASPNETCORE_ENVIRONMENT == Development` → yalnız `appsettings.Development.json`
- aksi → yalnız `appsettings.json`

Host `IConfiguration` (SuperAdmin bind) merge’lüdür; ikisi birden durur.

Mevcut değerler asıl projeden kopyalandı:

| Ortam | SQL | SOAP host (config, client yok) |
|---|---|---|
| Production | `SNKSQLFNS` / `EBA` | `eba.sanko.com.tr` |
| Development | `SNKSQLFNS.sankoholding.local` / `EBATEST` | `ebatest.sanko.com.tr` |

`EbaIntegrationInfo` config’de var; SOAP client henüz yok.

---

## 11. Auth iskeleti

- JWT: `JwtRestApi` sabitleri + `Jwt.GetJwt(...)` (`Application/Shared`)
- Password: `IPasswordHashService` (PBKDF2 / `KeyDerivation`)
- SuperAdmin: `IConfiguration.GetSection("SuperAdminUser")`
- `POST api/rest/auth/login2fa` açık (stub: SuperAdmin hash doğrula → token)
- Login/refresh tam iş kuralı ve kullanıcı tablosu **yok**
- `RoleType`: SuperAdmin / Admin / Member

---

## 12. Açık endpoint’ler

| Method | Path | Not |
|:------:|---|---|
| POST | `/api/rest/health-echo` | body echo + `SELECT 1`; boş Message → 422 |
| POST | `/api/rest/auth/login2fa` | SuperAdmin stub |
| GET | `/api/rest/SKY/get-environment` | `ASPNETCORE_ENVIRONMENT` |

---

## 13. İsim / ctor kuralları (yeni feature için)

- Dosya adı `IFooService.cs` / `IFooRepository.cs` — interface + class **aynı dosyada**
- Service ctor: yalnız interface (`new Repository()` yok)
- Repository ctor: boş; SQL için static `DbManager`
- Controller ctor: service interface

Checklist: Request → Response → Validator → Queries → Entity + `sColumn` → Repository → Service + `[ValidationAspect]` → Transient satırı → Controller.

---

## 14. Bilinçli olarak yok

eBA SOAP / `EbaHelper`, EF `DbContext`, MediatR, generic repository, Serilog, süreç endpoint’leri (SAT, masraf, envanter, techsheet, e-imza, toplu onay).
