# eBA Süreç Tetikleme Mekanizması

> **SanShine.Eba.Saas.Core** · REST API üzerinden eBA / eBA.net workflow motorunu uzaktan başlatır, ilerletir ve toplu onaylar.

| | |
|---|---|
| **Merkezi sınıf** | `SanShine.SaaS.Core.Contract/Shared/EbaHelper.cs` |
| **Giriş yüzeyi** | `api/rest/...` · JWT Bearer |
| **Tetikleme modeli** | Senkron HTTP — arka plan job / Hangfire / Quartz **yok** |
| **Ortam** | Prod: `eba.sanko.com.tr` · Test: `ebatest.sanko.com.tr` |

---

## İçindekiler

1. [Tek bakışta mimari](#1-tek-bakışta-mimari)
2. [Renkli süreç haritası](#2-renkli-süreç-haritası)
3. [İki SOAP yüzeyi](#3-iki-soap-yüzeyi)
4. [EbaHelper çekirdek API](#4-ebahelper-çekirdek-api)
5. [Dört tetikleme kalıbı](#5-dört-tetikleme-kalıbı)
6. [Süreç akışları](#6-süreç-akışları)
7. [HTTP endpoint kataloğu](#7-http-endpoint-kataloğu)
8. [Trigger parametre sözleşmesi](#8-trigger-parametre-sözleşmesi)
9. [Toplu onay kuralları](#9-toplu-onay-kuralları)
10. [Hata, log ve tutarlılık](#10-hata-log-ve-tutarlılık)
11. [Yeni süreç ekleme](#11-yeni-süreç-ekleme)

---

## 1. Tek bakışta mimari

İstek **asla** eBA’ya doğrudan gitmez. Controller → Service (iş kuralı) → `EbaHelper` (SOAP) ve/veya `DbManager` (EBA SQL).

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "fontFamily": "Segoe UI, sans-serif",
    "primaryTextColor": "#0f172a",
    "lineColor": "#64748b"
  }
}}%%
flowchart TB
    subgraph CLIENT[" İSTEMCİ "]
        SAP["SAP / Grup Şirket"]
        SF["SanFlow Backend"]
        MOB["Sanko Mobile"]
        ADM["SuperAdmin"]
    end

    subgraph API[" SanShine REST API · JWT "]
        PC["ProcessController"]
        IC["InventoryController"]
        PEC["PersonnelExpenseController"]
        TSC["TechSheetController"]
        ESC["ESignatureController"]
        DAC["DisSatinAlmaController"]
    end

    subgraph APP[" Application Services "]
        GS["GrupSirketService"]
        DS["DisSatinAlmaService"]
        INV["InventoryService"]
        PE["PersonnelExpenseService"]
        TS["TechSheetService"]
        ES["ESignatureService"]
        PS["ProcessService"]
    end

    subgraph HUB[" EbaHelper "]
        SP["StartProcess"]
        SPS["StartProcessWithStarter"]
        CP["ContinueProcess"]
        BP["BatchProcessContinue"]
        CF["CreateFormDocument"]
    end

    subgraph SOAP[" eBA SOAP "]
        LEG["eBAServiceReference<br/>/ebawsapi/ebawsapi.asmx"]
        NET["EbaNetServiceReference<br/>/eba.net/ws/ebawsapi.asmx"]
    end

    subgraph DB[" EBA SQL Server "]
        FORMS["Form tabloları E_*"]
        LF["LIVEFLOWS · FLOWREQUESTS"]
        OS["OSUSERS"]
    end

    SAP --> PC & DAC
    SF --> PEC
    MOB --> PS
    ADM --> TSC & ESC

    PC --> GS & DS & PS
    IC --> INV
    PEC --> PE
    TSC --> TS
    ESC --> ES
    DAC --> DS

    GS & INV & TS --> SP --> LEG
    PE --> CF --> LEG
    PE --> SPS --> NET
    DS --> CP --> LEG
    PS --> BP --> NET

    DS & PE --> FORMS
    GS & INV & ES --> OS
    DS & GS & ES --> LF
    ES -.->|"SOAP yok · yalnız SQL"| LF

    classDef client fill:#1e3a5f,stroke:#0f2744,color:#fff
    classDef api fill:#2563eb,stroke:#1d4ed8,color:#fff
    classDef app fill:#0d9488,stroke:#0f766e,color:#fff
    classDef hub fill:#7c3aed,stroke:#6d28d9,color:#fff
    classDef soap fill:#c2410c,stroke:#9a3412,color:#fff
    classDef db fill:#b45309,stroke:#92400e,color:#fff

    class SAP,SF,MOB,ADM client
    class PC,IC,PEC,TSC,ESC,DAC api
    class GS,DS,INV,PE,TS,ES,PS app
    class SP,SPS,CP,BP,CF hub
    class LEG,NET soap
    class FORMS,LF,OS db
```

### Katmanlar

| Renk | Katman | Proje | Görev |
|:----:|---|---|---|
| ![navy](https://img.shields.io/badge/--1e3a5f?style=flat-square) | İstemci | SAP · SanFlow · Mobil | İş tetikleyici |
| ![blue](https://img.shields.io/badge/--2563eb?style=flat-square) | Web API | `SanShine.Saas.Core.WebApi` | Route, JWT, rol |
| ![teal](https://img.shields.io/badge/--0d9488?style=flat-square) | Application | `SanShine.SaaS.Core.Application` | Validasyon + iş kuralı |
| ![violet](https://img.shields.io/badge/--7c3aed?style=flat-square) | Contract | `EbaHelper` | SOAP facade |
| ![orange](https://img.shields.io/badge/--c2410c?style=flat-square) | SOAP | `eBAServiceReference` / `EbaNetServiceReference` | eBA motoru |
| ![amber](https://img.shields.io/badge/--b45309?style=flat-square) | SQL | `SanShine.SaaS.Core.Infrastucture` | Form + kullanıcı + flow |

---

## 2. Renkli süreç haritası

Hangi endpoint hangi eBA projesini, hangi kalıpla tetikler.

```mermaid
%%{init: { "theme": "base", "themeVariables": { "fontFamily": "Segoe UI, sans-serif" } }}%%
flowchart LR
    subgraph A[" KALIP A · Trigger Start "]
        A1["GrupSapSatOnaySureci"]
        A2["Envanter_Sureci"]
        A3["NCS_TECHSHEET"]
    end

    subgraph B[" KALIP B · Form-first "]
        B1["NCS_MASRAF_SURECI_P_1ARALIK"]
    end

    subgraph C[" KALIP C · Continue "]
        C1["Dış SAT · event 20"]
        C2["Toplu onay · 5 süreç"]
    end

    subgraph D[" KALIP D · SQL iptal "]
        D1["E-imza · STATUS=25"]
    end

    classDef a fill:#2563eb,stroke:#1d4ed8,color:#fff
    classDef b fill:#7c3aed,stroke:#6d28d9,color:#fff
    classDef c fill:#0d9488,stroke:#0f766e,color:#fff
    classDef d fill:#dc2626,stroke:#b91c1c,color:#fff

    class A1,A2,A3 a
    class B1 b
    class C1,C2 c
    class D1 d
```

> [!IMPORTANT]
> **Dış satın alma başlatma endpoint’i bu API’de yok.** Süreç eBA içinde (veya başka kanalda) açılmış kabul edilir; burası yalnızca **devam** ve **malzeme parametresi** ekler.

---

## 3. İki SOAP yüzeyi

Aynı eBA ürününün iki WSDL’i bağlıdır. Yanlış client = starter / onaycı kaybolur.

```mermaid
%%{init: { "theme": "base" }}%%
flowchart TB
    CFG["appsettings · EbaIntegrationInfo"]
    CFG --> DEV{"IsDevops ?"}
    DEV -->|true| T["ebatest.sanko.com.tr"]
    DEV -->|false| P["eba.sanko.com.tr"]

    T --> L1["eBAServiceReference<br/>/ebawsapi/ebawsapi.asmx"]
    T --> N1["EbaNetServiceReference<br/>/eba.net/ws/ebawsapi.asmx"]
    P --> L2["eBAServiceReference<br/>/ebawsapi/ebawsapi.asmx"]
    P --> N2["EbaNetServiceReference<br/>/eba.net/ws/ebawsapi.asmx"]

    L1 & L2 --> USE1["StartProcess · ContinueProcess · CreateFormDocument"]
    N1 & N2 --> USE2["StartProcessWithStarter · BatchProcessContinue"]

    classDef cfg fill:#334155,stroke:#1e293b,color:#fff
    classDef test fill:#ca8a04,stroke:#a16207,color:#fff
    classDef prod fill:#16a34a,stroke:#15803d,color:#fff
    classDef use fill:#1d4ed8,stroke:#1e3a8a,color:#fff

    class CFG cfg
    class T,DEV test
    class P prod
    class L1,L2,N1,N2,USE1,USE2 use
```

| Proxy | Namespace | Ne zaman | Kimlik |
|---|---|---|---|
| **Klasik eBA** | `eBAServiceReference` | Süreç servis hesabı adına açılsın | `UserId` + `Password` + `Language` |
| **eBA.net** | `EbaNetServiceReference` | Süreç **gerçek kullanıcı** adına açılsın / onaylansın | Aynı + `targetUser` |

### Config anahtarları

`EbaIntegrationInfo` (değerler secret store’da tutulmalı; düz metin appsettings üretim için uygun değil):

| Anahtar | Anlam |
|---|---|
| `UserId` | eBA servis / entegrasyon hesabı |
| `Password` | Servis hesabı şifresi |
| `Language` | SOAP dil parametresi (`English`) |
| `Domain` | Klasik `ebawsapi` URL |
| `IsDevops` | `true` → test endpoint (`ebawsapiSoapDev12`) |

| Ortam | SQL | SOAP host |
|---|---|---|
| Production | `SNKSQLFNS` / `EBA` | `eba.sanko.com.tr` |
| Development | `SNKSQLFNS.sankoholding.local` / `EBATEST` | `ebatest.sanko.com.tr` |

API tarafı JWT (`Program.cs`); eBA tarafı her SOAP çağrısında servis hesabı. Token yenileme / OAuth yok.

---

## 4. EbaHelper çekirdek API

```mermaid
%%{init: { "theme": "base" }}%%
classDiagram
    class EbaHelper {
        +StartProcess(processName, obj, hasDefaultTrigger, hasVKey) ValidationResult
        +StartProcessWithStarter(processName, targetUserId, obj, ...) ValidationResult
        +ContinueProcess(maxRequest, processId, eventId) ValidationResult
        +ContinueProcess(client, maxRequest, processId, eventId, targetUserId) ValidationResult
        +DynamicCreateDocumentAtEbaAsync(processName, formName) int
        +BatchProcessContinue(request) BatchApprovalMethodResult
    }

    class ValidationResult {
        +bool Success
        +string Message
    }

    class ProcessApprovalRule {
        +string ProcessProjectName
        +string ProcessName
        +List~ApprovalStage~ Stages
        +GetDefaultRules() List
    }

    EbaHelper --> ValidationResult
    EbaHelper --> ProcessApprovalRule
```

### 4.1 `StartProcess` — reflection → `V_*`

Varsayılan: `hasDefaultTriggerValue = true`, `triggerValuesHasVKey = true`.

```text
obj.Property  ──►  WorkflowParameter { name: "V_{Property}", value: ToString() }
                   +  V_IsTriggered = "1"
                   ──►  StartProcessAsync(UserId, Password, Language, processName, params)
```

- `null` / boş property **gönderilmez**.
- Nested object / liste **desteklenmez** (sadece skaler `ToString()`).
- Başarıda `Message` = eBA `ProcessId`.
- Starter her zaman **servis hesabı**dır.

Kullanan: Grup şirket SAT · Envanter · TechSheet.

### 4.2 `StartProcessWithStarter` — eBA.net

```text
StartProcessAsync(UserId, Password, targetUserId, Language, WorkflowStartParameters)
```

Süreç `targetUserId` adına açılır. Masraf: `creatorUserDetails.EmployeeID`.

### 4.3 `ContinueProcess`

| Overload | Client | Kullanım |
|---|---|---|
| `(maxRequest, processId, eventId)` | Klasik + `LoginToken` | Dış SAT |
| `(client, maxRequest, processId, eventId, targetUserId)` | eBA.net, açık client | Toplu onay |

```text
ContinueProcessWithValidation(processId, requestId, eventId, reason="")
```

`eventId` = eBA formundaki buton id (ör. `20` = SAP cevap sonrası ilerlet).

### 4.4 `DynamicCreateDocumentAtEbaAsync`

Süreç başlamadan boş form belgesi üretir → `documentId`.

```text
LoginAsync → CreateFormDocumentAsync(loginToken, processName, formName)
```

Masraf: ana form `"Form"`, alt form `"DTY"`.

---

## 5. Dört tetikleme kalıbı

```mermaid
%%{init: { "theme": "base" }}%%
flowchart TB
    IN["HTTP + JWT"] --> BR{"Kalıp?"}

    BR -->|A| A["DTO → V_* parametre<br/>StartProcess"]
    BR -->|B| B["CreateFormDocument<br/>SQL form doldur<br/>StartProcessWithStarter"]
    BR -->|C| C["SQL form update<br/>ContinueProcess eventId"]
    BR -->|D| D["LIVEFLOWS finished<br/>form iptal + mail"]

    A --> OUT["eBA ProcessId"]
    B --> OUT
    C --> OUT
    D --> MAIL["Mail + STATUS=25"]

    classDef in fill:#1e3a5f,stroke:#0f2744,color:#fff
    classDef a fill:#2563eb,stroke:#1d4ed8,color:#fff
    classDef b fill:#7c3aed,stroke:#6d28d9,color:#fff
    classDef c fill:#0d9488,stroke:#0f766e,color:#fff
    classDef d fill:#dc2626,stroke:#b91c1c,color:#fff
    classDef out fill:#16a34a,stroke:#15803d,color:#fff

    class IN in
    class A a
    class B b
    class C c
    class D d
    class OUT,MAIL out
```

| Kalıp | Ad | SOAP | SQL | Örnek |
|:-----:|---|:----:|:---:|---|
| **A** | Trigger start | `StartProcess` | Kullanıcı / mükerrer kontrol | SAT, Envanter, TechSheet |
| **B** | Form-first | `CreateForm` + `StartProcessWithStarter` | Ana + alt form yazımı | Personel masraf |
| **C** | Continue | `ContinueProcessWithValidation` | Form update + `MaxRequestId` | Dış SAT, toplu onay |
| **D** | Soft cancel | — | `LIVEFLOWS` + form + log | E-imza iptal |

> [!WARNING]
> Kalıp B ve C’de **SQL ile SOAP aynı transaction değildir.** Form yazılır, SOAP fail ederse eBA’da yarım belge kalır. Retry / Polly yok.

---

## 6. Süreç akışları

### 6.1 Grup şirket SAP SAT onay — Kalıp A

`POST /api/rest/start-process-grup-sirket` · JWT

```mermaid
%%{init: { "theme": "base" }}%%
sequenceDiagram
    autonumber
    participant SAP as SAP / İstemci
    participant API as ProcessController
    participant SVC as GrupSirketService
    participant DB as EBA SQL
    participant H as EbaHelper
    participant EBA as eBA SOAP

    SAP->>API: JWT + GrupSirketSAPSatOnaySureciRequest
    API->>SVC: StartGrupSirketSAPSatOnaySureciAsync
    SVC->>SVC: FluentValidation
    SVC->>DB: SatNo daha önce süreç açmış mı?
    SVC->>DB: Owner + onaycılar OSUSERS aktif mi?
    alt Kural ihlali
        SVC-->>SAP: 4xx + mesaj
    else Geçerli
        SVC->>H: StartProcess("GrupSapSatOnaySureci", dto)
        H->>H: V_IsTriggered=1 + V_{Property}
        H->>EBA: StartProcessAsync(servis hesabı)
        EBA-->>H: ProcessId
        H-->>SVC: Success + ProcessId
        SVC-->>SAP: 200 ProcessOperationResponse
    end
```

**İş kuralları**

- `GroupCompanySatNo` → `E_GrupSapSatOnaySureci_Form` + `LIVEFLOWS` mükerrer kontrolü
- `RequestOwnerUserId` eBA’da aktif
- `ApproversUserId` (`;` ayrılmış) hepsi eBA’da aktif

**eBA parametreleri:** `V_IsTriggered`, `V_ApproversUserId`, `V_RequestOwnerUserId`, `V_GroupCompanySatNo`, `V_GroupCompany`, `V_GroupCompanyDescription`, `V_GroupCompanyMaterial`, `V_GroupCompanyMaterialDescription`, `V_HoldingSatMaterial`, `V_HoldingSatMaterialDescription`, `V_Currency`, `V_BidPrice`, `V_BidAmount`, `V_BidUnit`

---

### 6.2 Envanter — Kalıp A

`POST /api/rest/start-inventory-process` · JWT

```mermaid
%%{init: { "theme": "base" }}%%
flowchart LR
    R["SendInventoryInfoRequest"] --> V{"Owner email + aktif?<br/>Requester mevcut?"}
    V -->|Hayır| E["Hata mesajı"]
    V -->|Evet| S["StartProcess<br/>Envanter_Sureci"]
    S --> P["ProcessId"]

    classDef ok fill:#16a34a,stroke:#15803d,color:#fff
    classDef err fill:#dc2626,stroke:#b91c1c,color:#fff
    classDef mid fill:#2563eb,stroke:#1d4ed8,color:#fff
    class R mid
    class V mid
    class E err
    class S,P ok
```

Parametreler: `V_IsTriggered`, `V_RequesterUserId`, `V_OwnerUserId`, `V_Brand`, `V_Model`, `V_SerialNumber`

Liste: `GET /api/rest/get-all-inventory-processes` → `E_Envanter_Sureci_Form` + `LIVEFLOWS` (süreç tetiklemez).

---

### 6.3 Personel masraf — Kalıp B

`POST /api/rest/create-employee-expense-process` · JWT + **`sanflow-backend`**

En karmaşık orkestrasyon: form belgesi SOAP’ta açılır, içerik SQL ile basılır, süreç **oluşturan kullanıcı** adına başlar.

```mermaid
%%{init: { "theme": "base" }}%%
flowchart TB
    IN["CreatePersonnelExpenseProcessRequest"] --> VAL["Validasyon<br/>max 10 fiş · tek ödeme türü<br/>mükerrer · tarih/limit"]
    VAL --> USR["Creator + Owner<br/>email → OSUSERS"]
    USR --> MF["CreateFormDocument<br/>NCS_MASRAF_SURECI_P_1ARALIK / Form"]
    MF --> LOOP["Her kalem için<br/>CreateFormDocument · DTY"]
    LOOP --> SQL["SQL: ana form · alt form<br/>KDV · ana–alt bağ"]
    SQL --> LOOK["Lookup: vergi · belge tipi<br/>ödeme · kasa · masraf yeri"]
    LOOK --> ST["StartProcessWithStarter<br/>targetUser = creator EmployeeID"]
    ST --> OK["Süreç No döner"]

    VAL -.->|fail| X["400"]
    USR -.->|fail| X
    MF -.->|formId = -1| X
    SQL -.->|fail| X

    classDef step fill:#7c3aed,stroke:#6d28d9,color:#fff
    classDef ok fill:#16a34a,stroke:#15803d,color:#fff
    classDef err fill:#dc2626,stroke:#b91c1c,color:#fff
    class IN,VAL,USR,MF,LOOP,SQL,LOOK,ST step
    class OK ok
    class X err
```

**Ödeme türü haritası**

| CustomField | İç kod | Ana form | Alt form |
|---|---|---|---|
| `Vault` | `VAULT` | Kasa Ödemesi `05` | Kasa `05` |
| `CreditCard` | `CREDIT_CARD` | Şirket Kredi Kartı `02` | Şirket Kredi Kartı `01` |
| *(yok)* | `CASH` | Cari Hesap `01` | Cari Hesap `03` |

**SOAP start parametreleri**

| Parametre | Kaynak |
|---|---|
| `V_IsTriggered` | `"1"` |
| `V_CreatedFromApiMainFormId` | `CreateFormDocument` → `mainFormId` |
| `V_CuzdanGroupFormId` | `processRequest.GroupId` |

> [!CAUTION]
> Formlar açılıp süreç patlarsa **orphan form** kalır. Kodda rollback yoktur.

---

### 6.4 Teknik föy revize — Kalıp A (prefix yok)

`POST /api/rest/techsheet-pdf-revise-for-duplicate-test-data` · **`SuperAdmin`**

```mermaid
%%{init: { "theme": "base" }}%%
flowchart LR
    PROC["SQL prosedür<br/>revize listesi"] --> LOOP["foreach item"]
    LOOP --> P["StartProcess NCS_TECHSHEET<br/>TETIK SICIL REFID TIPKOD"]
    P --> WAIT["Thread.Sleep 4000 ms"]
    WAIT --> LOOP
    P --> L["StartedList / FailedList"]

    classDef a fill:#2563eb,stroke:#1d4ed8,color:#fff
    classDef w fill:#ca8a04,stroke:#a16207,color:#fff
    class PROC,LOOP,P,L a
    class WAIT w
```

Çağrı **bilinçli olarak** prefix’siz:

```csharp
EbaHelper.StartProcess("NCS_TECHSHEET", new {
    TETIK = "2",
    SICIL = item.CreatedBy,
    REFID = item.MasterFormId,
    TIPKOD = item.ArticleNumber
}, hasDefaultTriggerValue: false, triggerValuesHasVKey: false);
```

| Alan | eBA adı | Not |
|---|---|---|
| `TETIK` | `TETIK` | `"2"` = revize tetik |
| `SICIL` | `SICIL` | Formu oluşturan |
| `REFID` | `REFID` | Master form id |
| `TIPKOD` | `TIPKOD` | Article number |
| — | `V_IsTriggered` | **gönderilmez** |

4 sn bekleme eBA yükünü sınırlar. Tekil article revize (`StartProcessToTechSheetRevise`) endpoint’i **yorumda**, gövde TODO.

---

### 6.5 Dış satın alma devam — Kalıp C

`POST /api/rest/continue-process-dis-satin-alma` · JWT

```mermaid
%%{init: { "theme": "base" }}%%
sequenceDiagram
    autonumber
    participant C as İstemci / SAP
    participant S as DisSatinAlmaService
    participant DB as EBA SQL
    participant H as EbaHelper
    participant EBA as eBA SOAP

    C->>S: ProcessId + Materials + SapDocNo
    S->>DB: MAX ID FROM FLOWREQUESTS
    alt MaxRequestId yok
        S-->>C: Hata
    else
        S->>DB: UPDATE master txt_sapDocNo
        S->>DB: UPDATE satır fiyat / unique id
        S->>H: ContinueProcess(maxRequest, processId, 20)
        H->>EBA: Login + ContinueProcessWithValidation
        EBA-->>C: Süreç event 20 ile ilerler
    end
```

| DTO | eBA kolon |
|---|---|
| `SapDocNo` | `E_DisSatinAlmaSureci_Form.txt_sapDocNo` |
| `Materials[].ProfitablePrice` | `txt_profitablePrice` |
| `Materials[].TotalPrice` | `txt_totalPrice` |
| `Materials[].SapMaterialUniqueId` | `txt_sapMaterialUniqueId` |
| `RequirementNumber` + `MaterialUniqueId` | WHERE |

`POST /api/rest/add-material-dis-satin-alma` süreç **açmaz**; `E_DisSatinAlmaSureci_Prm_Materials_tbl_materials` insert eder.

---

### 6.6 Toplu onay — Kalıp C (eBA.net)

`POST /api/rest/batch-approval` · JWT + **`sanko-mobile`**

```mermaid
%%{init: { "theme": "base" }}%%
flowchart TB
    REQ["BatchApprovalRequest<br/>UserId · ProcessName · ProcessIds"] --> U{"Kullanıcı<br/>eBA'da aktif?"}
    U -->|Hayır| F["SetFailure"]
    U -->|Evet| R["ProcessApprovalRule eşle"]
    R --> W["GetUserWaitingProcessRequests<br/>targetUser = UserId"]
    W --> FLT["ProcessProjectName filtre"]
    FLT --> EACH["Her ProcessId"]
    EACH --> OWN{"Kullanıcı inbox'ında mı?"}
    OWN -->|Hayır| ERR["ErrorList"]
    OWN -->|Evet| STG["StatusId + ButtonId + 'Onayla'"]
    STG --> CONT["ContinueProcessWithValidation<br/>onaycı adına"]
    CONT --> OK["SuccessList"]

    classDef ok fill:#16a34a,stroke:#15803d,color:#fff
    classDef err fill:#dc2626,stroke:#b91c1c,color:#fff
    classDef mid fill:#0d9488,stroke:#0f766e,color:#fff
    class REQ,R,W,FLT,EACH,STG,CONT mid
    class F,ERR err
    class OK ok
```

`ProcessName` = **proje kodu** (`NCS_MASRAF_SURECI_P_1ARALIK`), görünen ad değil. Tanımsız ad reddedilir. Tek süreç hatası diğerlerini durdurmaz.

---

### 6.7 E-imza iptal — Kalıp D

`POST /api/rest/e-signature/process/cancel` · **`SuperAdmin`** veya **`e-signature`**

SOAP `CancelProcess` **yok**.

```mermaid
%%{init: { "theme": "base" }}%%
flowchart TB
    IN["UniqueId + UserId + ReasonText"] --> L1["LogCancelOperation"]
    L1 --> P["EIMZA_MASTER2 → süreç listesi"]
    P --> LF["LIVEFLOWS<br/>STATUS=25 · FINISHED=1"]
    P --> FR["E_NCS_EIMZA_Form<br/>iptal tarihi / kullanıcı / neden"]
    P --> ML{"Aktif + dummy değil?"}
    ML -->|Evet| MAIL["Eimza İptal mail"]
    ML -->|Hayır| DUM["DummyMails sözlüğü"]
    FR --> VER["EIMZA_MASTER2_Verification<br/>UniqueId = DEL-..."]
    MAIL --> LOG["Cancel log kapat"]
    DUM --> LOG
    VER --> LOG

    classDef d fill:#dc2626,stroke:#b91c1c,color:#fff
    classDef n fill:#334155,stroke:#1e293b,color:#fff
    class IN,L1,P,LF,FR,VER,LOG d
    class ML,MAIL,DUM n
```

Kritik SQL:

```sql
UPDATE LIVEFLOWS SET STATUS = 25, FINISHED = 1 WHERE ID = @id;
```

---

## 7. HTTP endpoint kataloğu

| Method | Path | Rol | Kalıp | eBA süreç / işlem |
|:------:|---|---|:-----:|---|
| `POST` | `/api/rest/start-process-grup-sirket` | JWT | A | `GrupSapSatOnaySureci` |
| `POST` | `/api/rest/start-inventory-process` | JWT | A | `Envanter_Sureci` |
| `POST` | `/api/rest/create-employee-expense-process` | `sanflow-backend` | B | `NCS_MASRAF_SURECI_P_1ARALIK` |
| `POST` | `/api/rest/techsheet-pdf-revise-for-duplicate-test-data` | `SuperAdmin` | A | `NCS_TECHSHEET` |
| `POST` | `/api/rest/continue-process-dis-satin-alma` | JWT | C | mevcut `processId` · event `20` |
| `POST` | `/api/rest/batch-approval` | `sanko-mobile` | C | kural tablosundaki 5 süreç |
| `POST` | `/api/rest/e-signature/process/cancel` | `e-signature` | D | uniqueId altındaki süreçler |
| `POST` | `/api/rest/add-material-dis-satin-alma` | JWT | — | parametre tablosu, süreç yok |

Swagger (lokal): `https://localhost:7191/swagger`

---

## 8. Trigger parametre sözleşmesi

eBA proje script’i (`OnProcessStart` / trigger) şu sözleşmeyi okumalıdır.

```mermaid
%%{init: { "theme": "base" }}%%
flowchart LR
    DTO["C# DTO property"] -->|"varsayılan"| V["V_{PropertyName}"]
    DTO -->|"TechSheet false,false"| RAW["ham ad TETIK / SICIL"]
    DEF["hasDefaultTriggerValue"] --> T["V_IsTriggered = 1"]

    classDef x fill:#2563eb,stroke:#1d4ed8,color:#fff
    classDef y fill:#ca8a04,stroke:#a16207,color:#fff
    class DTO,V,T x
    class RAW,DEF y
```

| Senaryo | Prefix | `V_IsTriggered` | Starter |
|---|---|---|---|
| SAT / Envanter | `V_` | evet | Servis hesabı |
| Masraf | `V_` | evet | **Gerçek kullanıcı** (`EmployeeID`) |
| TechSheet | yok | hayır | Servis hesabı |

eBA form designer’da **event / status id değişirse** API kırılır (`20`, `5`, `21` kodda magic number).

---

## 9. Toplu onay kuralları

`ProcessApprovalRule.GetDefaultRules()` — `EbaHelper.cs`

| `ProcessName` (istek) | Görünen ad | Aşama | ButtonId | StatusId |
|---|---|---|:--------:|:--------:|
| `NCS_MASRAF_SURECI_P_1ARALIK` | Personel Masraf | Yönetici | `5` | `21` |
| `NCS_MASRAF_SURECI_P_1ARALIK` | Personel Masraf | Akış Başlatan | `5` | `28` |
| `NCS_MASRAF_SURECI_SIRKET` | Şirket Masraf | Yönetici | `21` | `28` |
| `SapSatOnaySureci` | SAP Satın Alma Onay | Yönetici | `20` | `20` |
| `SeyahatFormu` | Görev Seyahat Talep | Yönetici | `5` | `31` |
| `izin_sureci` | İzin Talep | Yönetici | `5` | `37` |

Eşleşme: bekleyen request’in `Status.Id` + event `Id` + event `Text == "Onayla"`.

---

## 10. Hata, log ve tutarlılık

```mermaid
%%{init: { "theme": "base" }}%%
stateDiagram-v2
    [*] --> Validasyon: HTTP
    Validasyon --> IsKurali: FluentValidation OK
    Validasyon --> Fail: DTO hatalı
    IsKurali --> SOAP: kurallar geçti
    IsKurali --> Fail: kullanıcı / sat / limit
    SOAP --> OK: ProcessId
    SOAP --> Fail: SOAP / login
    OK --> [*]
    Fail --> [*]: BaseControllerResponse
```

| Konu | Davranış |
|---|---|
| SOAP hata | `ValidationResult.Success = false` + dosya log |
| Toplu onay | Kısmi başarı: `SuccessList` / `ErrorList` |
| TechSheet döngü | Kısmi: `StartedList` / `FailedList` |
| HTTP map | `CustomBaseController.HandleServiceResponse` |
| Retry | **Yok** |
| Dağıtık TX | **Yok** (SQL + SOAP ayrı) |
| Log | `Logs/{Yıl}/{Ay}/Api Çalışma Logs/` · masraf: `CuzdanIntegration_Logs` · föy: `TechSheet_Logs` |

Dokunan başlıca tablolar: `LIVEFLOWS`, `FLOWREQUESTS`, `FLOWDOCUMENTS`, `OSUSERS`, `E_GrupSapSatOnaySureci_Form`, `E_Envanter_Sureci_Form`, `E_DisSatinAlmaSureci_*`, `E_NCS_MASRAF_SURECI_P_1ARALIK_*`, `E_NCS_EIMZA_Form`, `EIMZA_MASTER2*`.

---

## 11. Yeni süreç ekleme

```mermaid
%%{init: { "theme": "base" }}%%
flowchart LR
    E1["1. eBA proje adı<br/>+ trigger değişkenleri"] --> E2["2. DTO property<br/>= trigger adı"]
    E2 --> E3["3. Service<br/>kurallar + EbaHelper"]
    E3 --> E4["4. Controller<br/>JWT / rol"]
    E4 --> E5["5. Toplu onay?<br/>ProcessApprovalRule"]
    E5 --> E6["6. Test<br/>IsDevops=true + EBATEST"]

    classDef s fill:#0d9488,stroke:#0f766e,color:#fff
    class E1,E2,E3,E4,E5,E6 s
```

**Karar ağacı**

| İhtiyaç | Kullan |
|---|---|
| Servis hesabı başlatsın, form eBA script’te dolsun | `StartProcess` · Kalıp A |
| Formu API doldursun, süreç kullanıcı adına açılsın | `CreateFormDocument` + `StartProcessWithStarter` · Kalıp B |
| Zaten açık süreçte butona basılsın | `ContinueProcess` · Kalıp C |
| Onaycı adına inbox’tan ilerletilsin | `BatchProcessContinue` + kural satırı |
| Süreci eBA motoru olmadan bitsin | SQL `LIVEFLOWS` · Kalıp D — dikkatli kullan |

---

## Referans dosyalar

| Dosya | Rol |
|---|---|
| `SanShine.SaaS.Core.Contract/Shared/EbaHelper.cs` | SOAP facade + onay kuralları |
| `SanShine.SaaS.Core.Common/Constants/EbaIntegrationInfo.cs` | Config modeli |
| `SanShine.Saas.Core.WebApi/Controllers/ProcessController.cs` | SAT start · SAT continue · batch |
| `SanShine.SaaS.Core.Application/Services/IGrupSirketSAPSatOnaySureciService.cs` | SAT iş kuralları |
| `SanShine.SaaS.Core.Application/Services/IDisSatinAlmaService.cs` | Continue + malzeme |
| `SanShine.SaaS.Core.Application/Services/IPersonnelExpenseService.cs` | Form-first masraf |
| `SanShine.SaaS.Core.Application/Services/IInventoryService.cs` | Envanter start |
| `SanShine.SaaS.Core.Application/Services/ITechSheetService.cs` | Föy revize döngüsü |
| `SanShine.SaaS.Core.Application/Services/IESignatureService.cs` | SQL iptal |
| `SanShine.SaaS.Core.Application/Services/IProcessService.cs` | Toplu onay orkestrasyonu |

---

*Kaynak: kod tabanı (`EbaHelper`, controller ve service katmanı). eBA form script’leri bu repoda değildir; trigger adları ve event id’ler eBA projesiyle senkron tutulmalıdır.*
