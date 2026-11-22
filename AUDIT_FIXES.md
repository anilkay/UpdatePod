# UpdatePod — LLMKodDenetci Audit Raporu Karşılık Kaydı

> **Kaynak rapor:** LLMKodDenetci Code Audit Report — Project: `updatepod`, Run ID: `7b01507b328a` (2026-11-22 15:00:23 UTC, 65 issue / 13 dosya)
> **Denetlenen kod durumu:** `ad765ff` (= `origin/main`, PR #10 merge) — `C:\Users\Administrator\AppData\Local\LLMKodDenetci\repos\updatepod-2947cfed` kopyası
> **Düzeltmelerin uygulandığı yer:** aynı içeriğe sahip git worktree, `ad765ff` üzerine
> **Karşılık tarihi:** 2026-11-22

> Not: Worktree başlangıçta daha eski bir commit'te (`56e64a8`) olduğu için önce denetlenen
> commit'e (`ad765ff`) checkout yapıldı; her issue gerçek kaynak kodla satır satır doğrulanıp
> öyle düzeltildi.

## Özet

| Kural | Ad | Seviye | Toplam | Çözüldü | Atlandı |
| :--- | :--- | :--- | ---: | ---: | ---: |
| `REL-001` | Disposable Not Disposed | Error | 6 | 6 | 0 |
| `REL-002` | Null Reference Risk | Warning | 2 | 2 | 0 |
| `MAINT-004` | Nested Depth | Warning | 2 | 2 | 0 |
| `STYLE-001` | Naming Conventions | Info | 41 | 26 | 15 |
| `MAINT-005` | Magic Numbers | Info | 14 | 14 | 0 |
| **Toplam** | | | **65** | **50** | **15** |

Ek doğrulama: projenin kendi `KNOWN_ISSUES.md` dosyası raporun iki bulgusunu bağımsız olarak
teyit ediyor — **L1** (response'lar dispose edilmiyor → Harbor/DockerIo'daki `REL-001`'ler) ve
**M4** (pod status null varsayımı → `REL-002`'ler). Bunlar kesin geçerli kabul edildi.

---

## Yapılanlar (50 issue)

### `UpdatePod.Domain\Utils\HttpClientUtils.cs` — 1 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `REL-001` [ERROR] | satır 16-17 | `CancelAfter`, `Timer.MaxSupportedTimeout`'tan (~49,7 gün) büyük timeout'larda `ArgumentOutOfRangeException` fırlatır (`IMAGE_OPERATIONS_TIMEOUT` env değişkeniyle ulaşılabilir); bu durumda bağlı CTS ve parent token'daki registration sızardı. → `try/catch` ile `cts.Dispose()` + rethrow eklendi. |

### `UpdatePod.Domain.Tests\HttpClientUtilsTests.cs` — 3 issue çözüldü, 3 atlandı

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `REL-001` [ERROR] | satır 34 | Assertion callback'i block lambda'ya çevrildi; dönen CTS `using var` ile sarıldı (metod beklenmedik şekilde dönerse sızıntı olmasın). |
| 3 | `MAINT-005` [INFO] | satır 11 | `100` → `const int cancellationDelayMilliseconds = 100` |
| 5 | `MAINT-005` [INFO] | satır 22 | `1` → `const int fallbackTimeoutMinutes = 1` |
| 2, 4, 6 | `STYLE-001` [INFO] | satır 8, 18, 31 | **Atlandı** (gerekçe: [Atlananlar](#atlananlar-15-issue) bölümü) |

### `UpdatePod.Domain\KubernetesUtils\KubernetesOperationWithKubernetesClient.cs` — 5 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `REL-001` [ERROR] | satır 24 | Sınıf `IDisposable` implemente etti: `public void Dispose() => _client.Dispose();`. Sahip olunan `Kubernetes` client'ı artık host kapanışında (singleton DI kaydı üzerinden) dispose ediliyor. |
| 2 | `REL-002` [WARNING] | satır 68 | `matchedPod.Status?.ContainerStatuses?.FirstOrDefault()` — eksik status verisinde NRE yerine mevcut `targetContainer == null` kontrolü anlamlı `ConstraintException` fırlatıyor. (`KNOWN_ISSUES.md` **M4** teyidi.) |
| 3 | `REL-002` [WARNING] | satır 72-73 | `matchedPod.Status?.ContainerStatuses?.FirstOrDefault(c => string.Equals(c.Name, containerName, ...))` — ayrıca null olabilecek `c.Name`'de `.Equals` yerine null-güvenli `string.Equals`. |
| 4 | `MAINT-004` [WARNING] | 159-174 | 5 seviyeli iç içe patch initializer'ı kaldırıldı. |
| 5 | `MAINT-004` [WARNING] | 183-198 | Aynı initializer `RestartStateFulSet`'te de kaldırıldı → her iki metod paylaşımlı `private static V1Patch CreateRestartPatch()` factory'sini kullanıyor (annotations → metadata → template → patchObject adımlarıyla yerel değişkenlerde kuruluyor). Üretilen JSON birebir aynı. |

### `UpdatePod.Domain\ImageOperations\ImageOperationsWithDockerIo.cs` — 2 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `REL-001` [ERROR] | satır 24 | `var response` → `using var response` (status doğrulaması veya JSON deserialize fırlatsa bile dispose garanti). (`KNOWN_ISSUES.md` **L1** teyidi.) |
| 2 | `MAINT-005` [INFO] | satır 39 | `parts.Length == 2` → `const int QualifiedImageNameSegmentCount = 2` |

Ayrıca model rename'ine bağlı olarak `content?.digest` → `content?.Digest` güncellendi (tek kullanım yeri).

### `UpdatePod.Domain\ImageOperations\ImageOperationsWithHarbor.cs` — 2 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `REL-001` [ERROR] | satır 34-35 | İstek + status kontrolü + digest okuma, response'u `using var` ile sahiplenen `GetDigestAsync` helper'ına taşındı. |
| 2 | `REL-001` [ERROR] | satır 40-41 | Fallback isteği de aynı helper'dan; ilk isteğin `EnsureSuccessStatusCode` fırlatması durumunda ilk response, fallback çalışmadan **önce** dispose edilir (eski kodda referans üzerine yazılıp sızıyordu). `digest?.ToList().FirstOrDefault()` → `digest?.FirstOrDefault()` (eşdeğer). |

> Not: HTTPS başarısızlığında http'ye düşme davranışı (`KNOWN_ISSUES.md` **H1** — güvenlik sorunu)
> **bilinçli olarak korundu**: rapor yalnızca disposal istiyordu, davranış değişikliği kapsam dışı.
> Bonus: `http://` URL'sini tutan yanıltıcı `httpsUrl` değişkeni `httpUrl` olarak adlandırıldı.

### `UpdatePod.Domain\ImageOperations\Models\DockerIoResponseModels.cs` — 26 issue (hepsi `STYLE-001`)

`DockerIoImageResponseModels` (16 property) ve `Images` (10 property) record'larındaki
snake_case public üyeler PascalCase'e alındı (`creator` → `Creator`, `last_updated` → `LastUpdated`,
`os_features` → `OsFeatures` vb.). Docker Hub'ın snake_case JSON alan adları
`[property: JsonPropertyName("...")]` attribute'larıyla korundu → **deserialization davranışı
birebir aynı** (canlı registry testleri geçerek kanıtladı). Gerekli `using System.Text.Json.Serialization;`
eklendi.

> Bu 26 `STYLE-001` geçerli kabul edildi çünkü bunlar public C# API üyeleri; test metodu
> adlarındaki alt çizgilerle aynı kategoride değerlendirilmedi.

### `UpdatePod.Domain.Tests\DockerIoResponseTests.cs` — 4 issue çözüldü, 4 atlandı

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `MAINT-005` [INFO] | satır 43 | `2` → `const int maxRetryAttempts = 2` |
| 2 | `MAINT-005` [INFO] | satır 44 | `5` → `const int baseRetryDelaySeconds = 5` |
| 4 | `MAINT-005` [INFO] | satır 66 | `71` → sınıf sabiti `ExpectedSha256DigestLength` |
| 6 | `MAINT-005` [INFO] | satır 85 | `71` → aynı sabit kullanıldı (71 = `"sha256:"` 7 karakter + 64 hex) |
| 3, 5, 7, 8 | `STYLE-001` [INFO] | satır 56, 72, 90, 111 | **Atlandı** (gerekçe: [Atlananlar](#atlananlar-15-issue) bölümü) |

**Bonus (raporda yok):** `DockerIoResponse_ShouldReturnDigestFromUserImamgeWithHash`
metodundaki `Imamge` yazım hatası, alt çizgi konvansiyonu korunarak
`DockerIoResponse_ShouldReturnDigestFromUserImageWithHash` yapıldı; `KNOWN_ISSUES.md`
içindeki M8 referansı da senkronlandı.

### `UpdatePod.Domain.Tests\QuayIoResponseTests.cs` — 3 issue çözüldü

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `MAINT-005` [INFO] | satır 20 | `2` → `const int maxRetryAttempts = 2` |
| 2 | `MAINT-005` [INFO] | satır 21 | `5` → `const int retryDelaySeconds = 5` |
| 3 | `MAINT-005` [INFO] | satır 38 | `71` → `const int expectedDigestLength = 71` |

### `UpdatePod\Program.cs` — 2 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `MAINT-005` [INFO] | satır 15 | `2` → `const int maxHttpRetryAttempts = 2` (top-level const) |
| 2 | `MAINT-005` [INFO] | satır 16 | `5` → `const int httpRetryBaseDelaySeconds = 5` (top-level const) |

### `UpdatePod.Domain\ImageOperations\Models\ImageOperationData.cs` — 1 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `MAINT-005` [INFO] | satır 24 | `?? 30` → `private const int DefaultImageOperationsTimeout = 30` |

### `UpdatePod.Domain\PodUpdateOperations\PodUpdateOptions.cs` — 1 issue

| # | Kural | Rapor | Yapılan |
| :--- | :--- | :--- | :--- |
| 1 | `MAINT-005` [INFO] | satır 16 | `10` → `private const int DefaultRestartIntervalMinutes = 10` |

---

## Atlananlar (15 issue — tamamı `STYLE-001`, test metodu adı PascalCase rename'leri)

**Gerekçe:** Alt çizgili test metodu adları xUnit ekosisteminde yerleşik ve yaygın bir
konvansiyondur. Projedeki **tüm** test metotları bu stili kullanıyor, repo'da bunu yasaklayan
bir `.editorconfig` / stil kuralı yok ve `KNOWN_ISSUES.md` (M8) testleri bu adlarla referans
veriyor. Rename pure kozmetik churn olur, davranışsal hiçbir faydası yok, dokümanlarla senkronu
bozar. Bu nedenle **false positive** kabul edildi.

| Dosya | Atlanan metotlar |
| :--- | :--- |
| `HttpClientUtilsTests.cs` | `CreateTimeoutTokenSource_CancelsAfterTimeout`, `CreateTimeoutTokenSource_CancelsWhenParentIsCancelled`, `CreateTimeoutTokenSource_ThrowsForNonPositiveTimeout` |
| `DockerIoResponseTests.cs` | `DockerIoResponse_ShouldReturnDigest`, `DockerIoResponse_ShouldReturnDigestFromUserImageWithHash`*, `DockerIoResponseWithToken_ShouldSendTokenAndReturnDigest`, `DockerIoResponseWithInvalidToken_ShouldThrow` |
| `ImageOperationDataTests.cs` | `FromEnvironment_ReadsAndTrimsValues`, `FromEnvironment_ReturnsNullsWhenUnset`, `FromEnvironment_ThrowsWhenUseHarborIsInvalid`, `FromEnvironment_ThrowsWhenTimeoutIsInvalid` |
| `PodUpdateOptionsTests.cs` | `FromEnvironment_ReadsAndTrimsValues`, `FromEnvironment_UsesDefaultsAndNullForBlankContainer`, `FromEnvironment_ThrowsWhenRequiredValueIsMissing`, `FromEnvironment_ThrowsWhenIntervalIsInvalid` |

(\*) PascalCase rename atlandı; yalnızca ad içindeki `Imamge` yazım hatası düzeltildi.

---

## Doğrulama

- `dotnet build UpdatePod.sln` → **Build succeeded — 0 Warning(s), 0 Error(s)**
- `dotnet test UpdatePod.sln` → **Passed! 22/22** (Failed: 0, Passed: 22, Skipped: 0)
- Canlı Docker Hub/Quay testleri de geçtiği için `DockerIoResponseModels` rename'inin JSON
  eşlemesini bozmadığı fiilen kanıtlandı.
- Değişiklikler commit'lenmedi; `git diff` ile incelenebilir.

## Kapsam dışı bırakılanlar (raporda yok, `KNOWN_ISSUES.md`'de belgelenmiş)

Audit raporunda yer almayan, ancak projenin kendi `KNOWN_ISSUES.md`'sinde listelenen ve bilinçli
olarak dokunulmayan daha derin konular:

- **H1** — Harbor'da HTTPS hatasında Basic credential ile plaintext http fallback (güvenlik)
- **H2** — Worker loop'ta exception handling yok; tek hata host'u durduruyor
- **M2/M3** — Singleton'ın `HttpClient`'ı yaklaması + shared default header mutasyonu
- **M5** — "Adı şununla başlayan ilk pod" seçimi nondeterministik
- **M6** — Harbor URL/tag string-replace parsing'i
- **M8** — Canlı registry çağıran testler CI'yı bloklayabilir
