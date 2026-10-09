# SaaS Core v2 — Altyapı Referansı

Kaynak repo: `SanShine.Eba.Saas.Core`  
Hedef: **yalnız sistem altyapısını** yeni projeye birebir kopyalamak.

Bu dosya domain / iş süreci / endpoint kataloğu **içermez**.  
Süreç tetikleme için ayrı dosya: [eba-surec-tetikleme.md](eba-surec-tetikleme.md)

---

## Cursor prompt (kopyala-yapıştır)

Yeni solution açıldığında (veya boş klasörde) aşağıdaki bloğu **aynen** ver. Bu dosyayı da chat’e ekle.

```text
Görevin: SanShine.SaaS.Core.V2 solution’ını SIFIRDAN kurmak.
Bu mesajdaki kuralları ve ekteki docs/saas-core-v2-altyapi-referans.md dosyasını
TEK KAYNAK kabul et. Tahmin etme; dosyada yazmayan şeyi uydurma.

KAPSAM (sadece altyapı):
- 5 proje: WebApi, Application, Contract, Infrastucture (yazım bilinçli), Common
- net6.0
- Autofac + Castle DynamicProxy AOP (ValidationAspect)
- FluentValidation + ValidationTool
- ExceptionMiddleware + custom exception tipleri
- LogHelper dosya log
- DbManager (ADO.NET, EF YOK — paket olsa bile kullanma)
- IResult / BusinessRules
- CustomBaseController + BaseControllerResponse
- Çift DI: Autofac SingleInstance + MS DI Transient
- JWT Bearer iskeleti (login endpoint iskeleti yeter)
- Program.cs pipeline birebir sıra
- Interface + implementation AYNI dosyada, dosya adı I*.cs
- Repository ctor boş / static DbManager
- Utilities.GetConfiguration tek dosya okur (merge YOK)

YAPMA:
- eBA süreçleri, SOAP client, EbaHelper, masraf/SAT/envanter/techsheet/e-imza kodu
- EF DbContext, MediatR, generic repository, yeni “best practice” katman
- Altyapıyı “iyileştirme”, isim düzeltme, Infrastucture typo’sunu düzeltme
- Serilog / ILogger geçişi (şimdilik LogHelper)

İLK TESLİM:
1. Solution + csproj referans/paket grafı
2. Common’daki interceptor, aspect, middleware, log, result, exception, Utilities
3. DbManager + sColumn + örnek HealthQueries / HealthEntity / IHealthRepository
4. IHealthService + HealthValidator + HealthRequest/Response
5. HealthController : CustomBaseController, POST api/rest/health-echo
6. Auth iskeleti: JwtRestApi sabitleri, JWT middleware, login2fa stub (hash + SuperAdmin config okuma)
7. TestController GET api/rest/SKY/get-environment
8. appsettings.json + appsettings.Development.json (secret placeholder)
9. AutofacApplicationModule + ApplicationModule + DataAccessModule
10. Çalışır derleme; swagger açılır

Yeni feature eklerken dosyadaki “Yeni feature checklist”i kullan.
İş bitince hangi dosyaları neden kopyaladığını kısa listele.
```

---

## 1. Solution grafı

```
SanShine.SaaS.Core.V2.sln
├── SanShine.Saas.Core.WebApi
├── SanShine.SaaS.Core.Application
├── SanShine.SaaS.Core.Contract
├── SanShine.SaaS.Core.Infrastucture    ← typo bilinçli, düzeltme
└── SanShine.SaaS.Core.Common
```

```
WebApi        → Application, Contract
Application   → Common, Infrastucture
Infrastucture → Common, Contract
Contract      → Common
Common        → yok
```

| Proje | SDK | Paket (minimum) |
|---|---|---|
| WebApi | Microsoft.NET.Sdk.Web | Autofac.Extensions.DependencyInjection 9.0.0, Swashbuckle.AspNetCore 6.5.0 |
| Application | Microsoft.NET.Sdk | Microsoft.AspNetCore.Cryptography.KeyDerivation 6.0.29, Otp.NET 1.4.0 |
| Contract | Microsoft.NET.Sdk | Microsoft.AspNetCore.Authentication.JwtBearer 6.0.28, Microsoft.Extensions.Configuration* 6.x, Newtonsoft.Json 13.0.3 |
| Infrastucture | Microsoft.NET.Sdk | Microsoft.Data.SqlClient 2.1.7 |
| Common | Microsoft.NET.Sdk | Autofac 8.0.0, Autofac.Extras.DynamicProxy 7.1.0, FluentValidation.AspNetCore 11.3.0, Microsoft.Data.SqlClient 2.1.7, Newtonsoft.Json 13.0.3 |

Hepsi: `net6.0`, `ImplicitUsings`, `Nullable`.  
WebApi `appsettings*.json` → `CopyToOutputDirectory`.

Kaynakta Contract’ta EF ve WCF paketleri vardır. **v2 iskeletinde ekleme.** Domain taşınırken ayrıca gelir.

---

## 2. Klasör sözleşmesi

### Common

```
Constants/
  JwtRestApi.cs
  LogConstants.cs
  SuperAdminCredentials.cs
Core/Aspects/Autofac/Validation/ValidationAspect.cs
Core/Aspects/Autofac/ExceptionHandling/DatabaseExceptionHandlingAspect.cs
Core/CrossCuttingConcerns/Validations/ValidationTool.cs
Core/Extensions/ExceptionMiddleware.cs
Core/Extensions/ExceptionMiddlewareExtensions.cs
Core/Extensions/ErrorDetails.cs
Core/Extensions/CustomExceptions/
  BusinessRulesException.cs
  DatabaseException.cs
  NotFoundException.cs
  ValidationErrorDetails.cs
Core/Utilities/LogHelper.cs
Core/Utilities/Utilities.cs
Core/Utilities/Business/BusinessRules.cs
Core/Utilities/Interceptors/
  MethodInterceptionBaseAttribute.cs
  MethodInterception.cs
  AspectInterceptorSelector.cs
Core/Utilities/Results/
  IResult.cs  Result.cs  SuccessResult.cs  ErrorResult.cs
  IDataResult.cs  DataResult.cs  SuccessDataResult.cs  ErrorDataResult.cs
```

### Application

```
DependencyResolvers/Autofac/AutofacApplicationModule.cs
Services/I{Name}Service.cs          ← interface + class aynı dosya
Shared/ResponseMessages.cs
Shared/Jwt.cs                       ← token üretimi
Enum/RoleType.cs
```

### Contract

```
Requests/{Domain}/XxxRequest.cs
Responses/BaseControllerResponse.cs
Responses/XxxResponse.cs
Validator/XxxValidator.cs           ← AbstractValidator<TRequest>
```

### Infrastucture

```
DbManagerRepository/DbManager.cs
Queries/{Domain}Queries.cs          ← static SQL const
Entities/{Domain}/XxxEntity.cs      ← [DbManager.sColumn("alias")]
Repositories/I{Name}Repository.cs   ← interface + class aynı dosya
```

### WebApi

```
Controllers/CustomBaseController.cs
Controllers/{Name}Controller.cs
Modules/ApplicationModule.cs
Modules/DataAccessModule.cs
Program.cs
appsettings.json
appsettings.Development.json
```

---

## 3. İsim ve ctor kuralları

- Dosya adı `IFooService.cs` / `IFooRepository.cs`. İçinde `interface IFoo` + `class Foo`.
- Service ctor: yalnızca interface. `new Repository()` yok.
- Repository ctor: boş. SQL için static `DbManager`.
- Controller ctor: service interface. `CustomBaseController` türet.
- Route: `[Route("api/rest/")]` + `[ApiController]`. Auth class’ta `[Authorize]`.
- Cevap tipi her zaman `BaseControllerResponse` türev. Controller `HandleServiceResponse` kullanır.

```csharp
public class FooService : IFooService
{
    private readonly IFooRepository _fooRepository;
    public FooService(IFooRepository fooRepository)
        => _fooRepository = fooRepository;
}
```

---

## 4. Program.cs sırası (değiştirme)

1. `AddControllers` + Swagger + Bearer security definition  
2. `UseServiceProviderFactory(AutofacServiceProviderFactory)` + `AutofacApplicationModule`  
3. `AddApplicationServices()`  
4. `AddDataAccess()`  
5. JWT Bearer (`JwtRestApi` sabitleri, `ClockSkew = Zero`)  
6. `AddAuthorization`  
7. `Build`  
8. `ConfigureCustomExceptionMiddleware()` — pipeline’ın başı  
9. `UseSwagger` / `UseSwaggerUI` (kaynakta Development if’i yorumlu; her ortam açık)  
10. `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapControllers`

---

## 5. Çift DI (birebir)

### A — Autofac (aspect bununla çalışır)

```csharp
builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
    .AsImplementedInterfaces()
    .EnableInterfaceInterceptors(new ProxyGenerationOptions
    {
        Selector = new AspectInterceptorSelector()
    })
    .SingleInstance();

builder.RegisterType<ValidationAspect>().SingleInstance();
builder.RegisterType<DatabaseExceptionHandlingAspect>().SingleInstance();
```

Application assembly taranır. Attribute **implementation metodunda** olmalı.

### B — MS DI Transient (elle liste)

`ApplicationModule.AddApplicationServices` → `services.AddTransient<IFooService, FooService>();`  
`DataAccessModule.AddDataAccess` → `services.AddTransient<IFooRepository, FooRepository>();`

İkisini de yaz. Autofac host olduğu için A kazanır; B kaynakta da var.

---

## 6. Aspect / validation

```
MethodInterceptionBaseAttribute : Attribute, IInterceptor
  └── MethodInterception   OnBefore → Proceed → OnException/OnSuccess → OnAfter
        └── ValidationAspect
```

`AspectInterceptorSelector`: class + method `MethodInterceptionBaseAttribute`, `Priority` sırası.

`ValidationAspect(Type validatorType)`:

- Tip `IValidator` değilse ctor exception: `"Bu bir doğrulama sınıfı değil"`
- `OnBefore`: `Activator.CreateInstance`, generic argüman = request tipi
- `invocation.Arguments` içinde **aynı runtime tip** olanları `ValidationTool.Validate`
- Fail → `FluentValidation.ValidationException`

```csharp
[ValidationAspect(typeof(FooValidator))]
public async Task<FooResponse> DoFooAsync(FooRequest request) { ... }
```

`ValidationTool.Validate`: `ValidationContext<object>` + `validator.Validate`; invalid ise throw.

`DatabaseExceptionHandlingAspect` `MethodInterceptionBaseAttribute` **değil** → selector görmez. Yine de register et (kaynak davranışı).

---

## 7. Exception middleware

`ExceptionMiddlewareExtensions.ConfigureCustomExceptionMiddleware` → `UseMiddleware<ExceptionMiddleware>`.

| Tip | HTTP | Body |
|---|---|---|
| `NotFoundException` | 404 | `ErrorDetails` + ErrorId; log satırı yok (erken return) |
| `ValidationException` | 422 | `ValidationErrorDetails` + Errors |
| `BusinessRulesException` | 400 | `ErrorDetails` |
| `DatabaseException` | 500 | genel DB mesajı |
| diğer | 500 | `Internal Server Error.` |

`ErrorId = Guid`. Path `api/rest/{x}` → log klasörü `SystemErrors/{x}`.  
JSON: Newtonsoft `ErrorDetails.ToString()`.

İki stil:

- Çoğu service **throw etmez** → `response.SetFailure` + StatusCode
- İstisna service **throw** eder → middleware

---

## 8. Result + BusinessRules

```csharp
public interface IResult { bool Success { get; set; } string Message { get; set; } }
public class Result : IResult { ... }
public class SuccessResult : Result { public SuccessResult() : base(true) {} }
public class ErrorResult : Result { public ErrorResult(string message) : base(false, message) {} }
```

`BusinessRules.Run(params IResult[])`: ilk `Success == false` döner, hepsi geçerse `null`.

```csharp
var rule = BusinessRules.Run(await CheckA(), CheckB());
if (rule != null) { response.Message = rule.Message; return response; }
```

---

## 9. Controller + response

`BaseControllerResponse`: `Message`, `[JsonIgnore] StatusCode` default `BadRequest`, `SetSuccess` / `SetFailure` + generic factory.

`CustomBaseController.HandleServiceResponse<T>(Func<Task<T>>)` where `T : BaseControllerResponse`:

- null method → `ArgumentNullException`
- null response → 500
- map: OK, Created, NoContent, BadRequest, Unauthorized, Forbid, NotFound, Conflict, else StatusCode

```csharp
[HttpPost("do-foo")]
public async Task<IActionResult> DoFoo([FromBody] FooRequest request)
    => await HandleServiceResponse(() => _fooService.DoFooAsync(request));
```

---

## 10. LogHelper

`LogHelper.WriteToFileAsync(msg, fileName, folderName)`  
Yol: `{cwd}/Logs/{Year}/{Month}/{folderName}/{dd.MM.yyyy}_{fileName}.txt`  
Satır: saat + `[CallerMemberName]` + mesaj. Hata → `Console.WriteLine`, swallow.

`LogConstants`: `FolderName = "Api Çalışma Logs"`, `CommonFileName = "Logs"`.

ILogger / Serilog yok.

---

## 11. DbManager

Static. Connection: `Utilities.GetConfiguration().GetConnectionString("DefaultConnection")`.

| Metod | Anlam |
|---|---|
| `ReadAsync<T>(sql, dict)` | SELECT → `List<T>`, `T : new()` |
| `ExecuteNonQueryAsync` | `rowsAffected > 0` |
| `ExecuteNonQueryTransactionAsync` | tek komut TX; **yalnız 1 row** commit |
| `RunProcedureAsync` | SP; `rowsAffected == -1` → true |

Parametre: `Dictionary<string, object>` + `AddWithValue`.  
Map: yalnız `[DbManager.sColumn("alias")]` olan property. Alias yoksa kolon atlanır.

```csharp
[DbManager.sColumn("eba_user_id")]
public string EbaUserId { get; set; }
```

Query: `internal static class FooQueries { internal const string GetById = @"SELECT ..."; }`

EF **kullanma**.

---

## 12. Config

`Utilities.GetConfiguration()`:

- `ASPNETCORE_ENVIRONMENT == "Development"` → **sadece** `appsettings.Development.json`
- aksi / null → **sadece** `appsettings.json`

Merge yok. `DbManager` (ve kaynakta EbaHelper) static ctor bunu kullanır.  
Host `IConfiguration` (ör. SuperAdmin) merge’lüdür. İkisini de koru.

```json
{
  "ConnectionStrings": { "DefaultConnection": "Server=.;Database=...;User Id=...;Password=...;" },
  "SuperAdminUser": {
    "Id": "-1",
    "Username": "admin@local",
    "Password": "<hashed>",
    "Role": "Super_Admin",
    "RoleId": "-10"
  }
}
```

`JwtRestApi`: `SignKey`, `ValidIssuer`, `ValidAudience`, `TokenExpiration`, `RefreshTokenExpiration`.

---

## 13. Auth iskeleti (domain değil)

- `POST api/rest/auth/login2fa` açık
- JWT üretimi `Jwt.GetJwt(...)` (Application/Shared)
- SuperAdmin: `IConfiguration.GetSection("SuperAdminUser")`
- Password: `IPasswordHashService` (KeyDerivation)
- `[Authorize]` / `[Authorize(Roles="...")]`
- `RoleType` enum

Login/refresh tam iş kuralını kopyalamak zorunlu değil; middleware + stub + SuperAdmin okuma yeterli. Sonra taşınır.

---

## 14. Yeni feature checklist

1. `Contract/Requests/{D}/XRequest.cs`  
2. `Contract/Responses/XResponse.cs` : `BaseControllerResponse`  
3. `Contract/Validator/XValidator.cs` : `AbstractValidator<XRequest>`  
4. `Infrastucture/Queries/XQueries.cs`  
5. `Infrastucture/Entities/{D}/XEntity.cs` + `sColumn`  
6. `Infrastucture/Repositories/IXRepository.cs` (interface+class, boş ctor)  
7. `Application/Services/IXService.cs` — ctor repo, `[ValidationAspect]`, `BusinessRules`, response  
8. `ApplicationModule` + `DataAccessModule` Transient satırı  
9. Autofac assembly tarama (ek kayıt gerekmez)  
10. `WebApi/Controllers/XController.cs` : `CustomBaseController` + `HandleServiceResponse`

İlk örnek feature: **Health**

- `POST /api/rest/health-echo` — body echo + `SetSuccess`
- Validator: Message boş olmasın
- İsteğe bağlı: `DbManager.ReadAsync` ile `SELECT 1` (bağlantı varsa)

Ayrıca: `GET /api/rest/SKY/get-environment` → `ASPNETCORE_ENVIRONMENT`

---

## 15. Kopyalanacak tuhaflıklar (düzeltme)

| # | Koru |
|---|---|
| 1 | Klasör/proje adı `Infrastucture` |
| 2 | Interface+impl aynı dosya, `I*.cs` |
| 3 | Autofac SingleInstance + MS DI Transient |
| 4 | `DatabaseExceptionHandlingAspect` register, attribute olarak kullanılmıyor |
| 5 | `GetConfiguration` tek dosya |
| 6 | TX metodu yalnız 1 satırda commit |
| 7 | Service fail çoğunlukla throw değil |
| 8 | JWT / SuperAdmin config düz metin |

---

## 16. Kaynak dosyalar (altyapı)

```
SanShine.SaaS.Core.Common/
  Core/Utilities/Interceptors/*
  Core/Aspects/Autofac/Validation/ValidationAspect.cs
  Core/Aspects/Autofac/ExceptionHandling/DatabaseExceptionHandlingAspect.cs
  Core/CrossCuttingConcerns/Validations/ValidationTool.cs
  Core/Extensions/ExceptionMiddleware.cs
  Core/Extensions/ExceptionMiddlewareExtensions.cs
  Core/Extensions/ErrorDetails.cs
  Core/Extensions/CustomExceptions/*
  Core/Utilities/LogHelper.cs
  Core/Utilities/Utilities.cs
  Core/Utilities/Business/BusinessRules.cs
  Core/Utilities/Results/*
  Constants/JwtRestApi.cs
  Constants/LogConstants.cs
  Constants/SuperAdminCredentials.cs

SanShine.SaaS.Core.Application/
  DependencyResolvers/Autofac/AutofacApplicationModule.cs
  Shared/ResponseMessages.cs
  Shared/Jwt.cs
  Services/IPasswordHashService.cs
  Enum/RoleType.cs

SanShine.SaaS.Core.Contract/
  Responses/BaseControllerResponse.cs
  Validator/  (şablon: GrupSirketValidator yapısı, içeriği değil)

SanShine.SaaS.Core.Infrastucture/
  DbManagerRepository/DbManager.cs

SanShine.Saas.Core.WebApi/
  Program.cs
  Controllers/CustomBaseController.cs
  Modules/ApplicationModule.cs
  Modules/DataAccessModule.cs
```

Kopyalama sırasında **servis/query/entity içeriğini** (SAT, masraf, eBA SOAP) alma. Yalnız kalıp.
