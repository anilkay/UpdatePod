# UpdatePod — Known Issues and Review Notes

> Reviewed on **2026-11-15** against `main` (`402a1ad`, after the .NET 11 upgrade).
>
> The .NET 11 migration itself is complete: a clean Release build has **0 warnings / 0 errors**, the test suite passes, and CI builds multi-arch images. This document covers the remaining security, reliability, correctness, CI, and documentation issues found during the review.
>
> The README marks the tool as **not for production**. The items below matter if that changes, or if the tool runs unattended for long periods.
>
> **Update:** M1 and M7 were fixed in the same PR that adds this document. H1 and M11 were fixed later. Their entries are kept for context and marked **Fixed**.

## Severity

| Level | Meaning |
| --- | --- |
| **High** | Security exposure or a service-outage risk; address before relying on the tool. |
| **Medium** | Can cause intermittent failures, resource leaks, wrong decisions, or block releases. |
| **Low** | Hardening, maintainability, or documentation. |

## Summary

| # | Severity | Area | Issue |
| --- | --- | --- | --- |
| H1 | High | Security | ✅ Fixed — Harbor lookups are HTTPS-only; the plain-HTTP fallback was removed |
| H2 | High | Reliability | Any background-loop exception stops the whole host |
| M1 | Medium | Reliability | ✅ Fixed in this PR — timeout `CancellationTokenSource`s are disposed |
| M2 | Medium | Reliability | Singleton captures one `HttpClient`, defeating handler/DNS rotation |
| M3 | Medium | Correctness/Security | Shared `HttpClient` default headers are mutated per call and can leak between registries |
| M4 | Medium | Reliability | Pod/container statuses are assumed to be non-null |
| M5 | Medium | Correctness | "First pod whose name starts with…" is nondeterministic |
| M6 | Medium | Correctness | Harbor URL/tag construction is string-replace based and breaks on valid names |
| M7 | Medium | Reliability | ✅ Fixed in this PR — environment variables are validated at startup |
| M8 | Medium | CI | Remaining tests call live Docker Hub/Quay and can block image publishing |
| M9 | Medium | CI | Dockerfile changes are not built on pull requests |
| M10 | Medium | Kubernetes | No retry/backoff for Kubernetes API calls |
| M11 | Medium | Docs/RBAC | ✅ Fixed — README/RBAC describe the current code and registry |
| L1 | Low | Reliability | `HttpResponseMessage` instances are not disposed |
| L2 | Low | Correctness | Container-name comparisons are inconsistent |
| L3 | Low | Code quality | Generic exceptions and typos in the Kubernetes layer |
| L4 | Low | Observability | Log/error messages lose structured data or name the wrong object |
| L5 | Low | Correctness | Registry detection uses substring matching |
| L6 | Low | Supply chain | Actions are tag-pinned; SDK is not pinned with `global.json` |
| L7 | Low | Container | Floating base tags, broad build context, no health check |
| L8 | Low | Dependencies | `KubernetesClient` is four majors behind (patched, but plan an upgrade) |
| L9 | Low | Legal | No `LICENSE` file |

---

## High

### H1. Harbor HTTPS failures silently retry over HTTP with Basic credentials (fixed)

**Status:** Fixed. `ImageOperationsWithHarbor` now sends one HTTPS request and never falls back to `http://`; transient failures are left to the resilience handler. Plain-HTTP registries are not supported (documented in the README). `HarborImageOperationsTests` cover 401/404/503 and transport errors.

**Files:** `UpdatePod.Domain/ImageOperations/ImageOperationsWithHarbor.cs:33-43`

The `try` block wraps both `GetAsync` and `EnsureSuccessStatusCode()`. The `catch (HttpRequestException)` then retries the same URL with `http://`. That means **any** HTTPS failure — TLS/certificate error, DNS error, 401/404, or 5xx — triggers a plaintext request.

When Harbor is enabled, `Authorization: Basic ...` is set as a default header, so robot credentials can be sent unencrypted and are exposed to a man-in-the-middle. The fallback also makes an authentication or missing-manifest response retry over HTTP, which is never desirable.

**Fix:** remove the automatic downgrade. If an insecure Harbor is genuinely required, make it an explicit opt-in (for example `USE_INSECURE_HARBOR=true`) and refuse to send credentials over HTTP. Retry only on transient transport errors, never because of an HTTP status code.

### H2. Any background-loop exception stops the whole host

**Files:** `UpdatePod/Worker.cs:16-31`, `UpdatePod.Domain/PodUpdateOperations/PodUpdateOperationWithDomain.cs:50-56`

The worker loop has no per-iteration exception handling. `BackgroundServiceExceptionBehavior` defaults to `StopHost`, so a single transient Kubernetes 404/timeout, an unreachable registry, or a pod with `imagePullPolicy != Always` terminates the process. This was observed during the upgrade smoke test: a Kubernetes 404 from `ListNamespacedPodAsync` stopped the host.

**Fix:** validate configuration at startup (fail fast for missing/invalid environment variables), then wrap each iteration in `try/catch`, log the error, and continue after the delay. Treat `OperationCanceledException` during shutdown as normal. Keep fatal configuration errors separate from retryable runtime errors.

---

## Medium

### M1. Timeout `CancellationTokenSource`s were not disposed (fixed in this PR)

**File:** `UpdatePod.Domain/Utils/HttpClientUtils.cs:5-9`

**Status:** Fixed in this PR. `HttpClientUtils.CreateTimeoutTokenSource` now returns a disposable linked source, both registry strategies dispose it with `using`, and non-positive timeouts are rejected. Tests cover timeout, parent cancellation, and invalid values.

Every call creates a `CancellationTokenSource(timeout)` and a linked CTS, returns only the token, and disposes neither:

```csharp
var cts = new CancellationTokenSource(timeout);
return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;
```

The linked registration stays attached to the long-lived worker token, and the timeout timer stays alive until GC. Over a long-running process this accumulates registrations and timers.

**Fix:** let the caller own the lifetime:

```csharp
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(timeout);
var response = await httpClient.GetAsync(url, cts.Token);
```

Alternatively return a disposable wrapper that owns both sources.

### M2. Singleton captures one `HttpClient`, defeating handler/DNS rotation

**Files:** `UpdatePod/Program.cs:9,47`, `UpdatePod.Domain/ImageOperations/ImageOperations.cs:12`

`ImageOperations` is registered as a singleton, but it receives the transient `HttpClient` created by `AddHttpClient()`. The singleton keeps that client (and its handler) forever, so `HttpClientFactory`'s handler rotation and DNS refresh no longer apply. A registry endpoint change or DNS failover requires a pod restart.

**Fix:** inject `IHttpClientFactory` and create a client per operation, or register a typed client and make `ImageOperations` scoped/transient. Keep singleton state separate from the HTTP client.

### M3. Shared `HttpClient` default headers are mutated per call and can leak between registries

**Files:** `UpdatePod.Domain/ImageOperations/ImageOperationsWithDockerIo.cs:13,17`, `UpdatePod.Domain/ImageOperations/ImageOperationsWithHarbor.cs:16,24-30`

Both strategies mutate `httpClient.DefaultRequestHeaders` on the same shared client:

- Docker adds `Accept: application/json` on **every** call; repeated calls keep adding duplicate values.
- Docker sets `Authorization: JWT ...` globally when `DOCKER_HUB_TOKEN` is present.
- Harbor sets `Authorization: Basic ...` globally when Harbor is enabled and clears/adds `Accept`.

In the current single-image flow only one strategy is used, but the default headers survive across calls and would leak credentials/Accept values if both registries are ever used (or if the monitored image changes registry). `DefaultRequestHeaders` is not intended to be mutated while requests are in flight.

**Fix:** put `Accept`/`Authorization` on each `HttpRequestMessage` (or use one client per registry) instead of the shared default headers. Clear/overwrite headers explicitly when a client is reused.

### M4. Pod/container statuses are assumed to be non-null

**File:** `UpdatePod.Domain/KubernetesUtils/KubernetesOperationWithKubernetesClient.cs:64-78` (same pattern in `GetImagePullPolicy`, around lines 96-105)

```csharp
targetContainer = matchedPod.Status.ContainerStatuses.FirstOrDefault();
```

Both `Status` and `ContainerStatuses` can be absent on a pending/just-created pod, in which case this throws `NullReferenceException`. `GetImageWithDigest` also throws when `ImageID` does not contain `@sha256:` yet, which is normal during pod startup.

**Fix:** null-check `Status`/`ContainerStatuses`, skip pods that are not ready or have no status, and treat "digest not available yet" as a retryable condition instead of an exception.

### M5. "First pod whose name starts with…" is nondeterministic

**File:** `UpdatePod.Domain/KubernetesUtils/KubernetesOperationWithKubernetesClient.cs:29-31` (same pattern in every lookup method)

All pod lookups list every pod in the namespace and take the first name that starts with the prefix. The order is not guaranteed, terminating pods are included, and during a rolling update the loop can alternate between old and new pods with different digests. Large namespaces also pay for a full list on every interval.

**Fix:** use a `labelSelector` (and/or `fieldSelector`), ignore pods with `DeletionTimestamp`, and choose deterministically (for example the newest ready pod) or require an exact pod name.

### M6. Harbor URL/tag construction is string-replace based and breaks on valid names

**Files:** `UpdatePod.Domain/ImageOperations/ImageOperationsWithHarbor.cs:51-63`, `UpdatePod.Domain/ImageOperations/ImageOperations.cs:56-63`

`GetUrl` removes the host and tag with `string.Replace`, which also rewrites matching text in the repository path:

```text
harbor.example.com/team/app-1.0:1.0
→ /v2/team/app-/manifests/1.0   (wrong repository)
```

When the image has no tag, `ParseImageForHarbor` uses the last path segment as the tag, so `harbor.example.com/team/app` produces `manifests/app` instead of `manifests/latest`.

**Fix:** parse the image reference once (registry, repository, tag/digest) and build the URL with `Uri`/`UriBuilder`; default an omitted tag to `latest`. Add tests for registry-with-port, nested paths, and missing tags.

### M7. Numeric environment variables were not validated (fixed in this PR)

**Files:** `UpdatePod/Worker.cs:43-45`, `UpdatePod/Program.cs:34-35`, `UpdatePod.Domain/ImageOperations/Models/ImageOperationData.cs:10-12`

**Status:** Fixed in this PR. Environment parsing is centralized in `EnvironmentConfiguration`; `PodUpdateOptions.FromEnvironment` and `ImageOperationData.FromEnvironment` validate required values, positive integers, and booleans before the host starts. Tests cover defaults, trimming, and invalid values.

`RESTART_INTERVAL_MINUTES` is parsed with `int.TryParse` and used directly:

- `0` produces a hot loop with no delay.
- A negative value makes `Task.Delay` throw.

`IMAGE_OPERATIONS_TIMEOUT` has the same problem: `0` cancels immediately, and a negative value makes `TimeSpan.FromSeconds` throw. Missing `POD_NAMESPACE` / `POD_NAME_PREFIX` throws inside the loop, which (with H2) stops the host.

**Fix:** validate at startup (`IOptions` + `ValidateOnStart`, or explicit checks), require interval >= 1 and timeout > 0, and log one clear configuration error instead of crashing per iteration.

### M8. Remaining tests call live Docker Hub/Quay and can block image publishing

**Files:** `UpdatePod.Domain.Tests/DockerIoResponseTests.cs:38-70`, `UpdatePod.Domain.Tests/QuayIoResponseTests.cs:10-40`

Three tests still call live registries:

- `DockerIoResponse_ShouldReturnDigest`
- `DockerIoResponse_ShouldReturnDigestFromUserImageWithHash`
- `QuayIoResponse_ShouldReturnDigest`

They can fail on rate limits, network restrictions, registry outages, or changed public images. The `docker` job depends on the test job, so an external-service failure blocks image publishing. The token/invalid-token tests are deterministic now, but the suite as a whole is not.

**Fix:** mock `HttpMessageHandler` for unit tests; keep live calls as `[Trait("Category", "Integration")]` and run them on a schedule or with explicit credentials. Add unit tests for image parsing and the restart decision.

### M9. Dockerfile changes are not built on pull requests

**File:** `.github/workflows/ci.yml:49`

The `docker` job is skipped on `pull_request`:

```yaml
if: github.event_name != 'pull_request'
```

A PR that breaks the Dockerfile passes checks and only fails after merge (or on a branch push in the same repository). The Dockerfile fixes in the .NET 11 PR were validated by a branch push, but external contributions do not get that check.

**Fix:** on PRs, build `linux/amd64` with `push: false`; keep the multi-arch build for `main`/tags. Path filters can limit it to changes in `UpdatePod/Dockerfile`, `Directory.*`, or project files.

### M10. No retry/backoff for Kubernetes API calls

**File:** `UpdatePod.Domain/KubernetesUtils/KubernetesOperationWithKubernetesClient.cs`

Polly is configured on the DI `HttpClient` (`UpdatePod/Program.cs:11-23`), but `KubernetesClient` creates its own HTTP pipeline and does not use that client. Kubernetes API 429/500 responses and timeouts are not retried; combined with H2, they stop the service.

**Fix:** configure a retrying `DelegatingHandler` in `KubernetesClientConfiguration` (or wrap the calls with a retry policy) and honor `Retry-After` for 429.

### M11. README/RBAC do not match the current code and registry (fixed)

**Status:** Fixed. The README now documents GHCR, every environment variable, the digest comparison and StatefulSet path, the `imagePullPolicy: Always` requirement, the supported registries and limitations, secrets via shell variables and a Kubernetes `Secret`, and `statefulsets` RBAC.

**File:** `README.md:13-17,23-28,40,54-70,164-202`

- The image section only documents `docker pull aanilkay/updatepod:latest`; CI now publishes `ghcr.io/anilkay/updatepod`.
- The environment table is missing `USE_HARBOR`, `HARBOR_ROBOT_USER`, `HARBOR_ROBOT_TOKEN`, `DOCKER_HUB_TOKEN`, and `IMAGE_OPERATIONS_TIMEOUT`.
- "What It Does" says Pod → ReplicaSet → Deployment; the code also restarts StatefulSets.
- The RBAC example grants `pods`, `replicasets`, and `deployments` permissions but no `statefulsets` permissions. StatefulSet restarts will fail with 403.
- The `launchSettings.json` example puts Harbor/Docker credentials in a tracked file. That invites committing secrets; recommend `dotnet user-secrets` locally and a Kubernetes `Secret` in deployments.
- The `imagePullPolicy: Always` requirement is not documented, although the code throws otherwise.

**Fix:** update the README/RBAC and add a short "requirements/limitations" section.

---

## Low

### L1. `HttpResponseMessage` instances are not disposed

`ImageOperationsWithDockerIo.GetLatestHash` and `ImageOperationsWithHarbor.GetLatestHash` keep the response without `using`. Dispose responses after reading them to release resources promptly and make the ownership explicit.

### L2. Container-name comparisons are inconsistent

`GetPodContainerImageInfo` and `GetImagePullPolicy` compare names with `==` (case-sensitive), while `GetPodContainerImageHash` uses `OrdinalIgnoreCase`. Standardize on one comparison (Kubernetes names are DNS labels, but the inconsistency is unnecessary) and extract a helper.

### L3. Generic exceptions and typos in the Kubernetes layer

- `GetStateFulSetFromPod` throws a generic `Exception`; use `KubernetesObjectNotFoundException` or a dedicated exception so callers can handle it consistently.
- The constructor catches all exceptions from `InClusterConfig()` and silently falls back to `BuildDefaultConfig()`; log why the fallback happened.
- `GetStateFulSetFromPod` / `RestartStateFulSet` are misspelled; rename to `StatefulSet`.

### L4. Log/error messages lose structured data or name the wrong object

- `PodUpdateOperationWithDomain.LogImageHashes` uses string interpolation; use message templates (`{PodImageHash}`) so structured logging works.
- `ValidateLatestImageHash` formats `containerName` (possibly `null`) instead of the image name.
- `ApplicationException` is used for expected runtime conditions; consider a dedicated exception type or a result type.

### L5. Registry detection uses substring matching

`ImageOperations.GetLatestHashFromImage` decides the registry with `image.Contains("docker.io", ...)`, which also matches `docker.io.example.com/...` or a path containing that text. Parse the image reference and compare the registry host exactly.

### L6. Supply-chain and toolchain reproducibility

- Actions are pinned to major tags (`@v7`, `@v6`, …), not commit SHAs. Pin to SHAs and let Dependabot/Renovate update them.
- `ubuntu-latest` is migrating to Ubuntu 26; pin a specific runner image for reproducible builds.
- There is no `global.json`; CI installs `11.0.x`, which can pick a different SDK than local (`11.0.100`). Add `global.json` with `rollForward: latestPatch`.
- The Docker job runs a multi-arch build for every branch push; consider path filters or limit it to `main`/tags and Dockerfile PRs.

### L7. Container hardening

- `runtime:11.0` / `sdk:11.0` are floating tags; pin digests (or use Renovate) for reproducible images.
- `.dockerignore` excludes `bin`, `obj`, and `README.md`, but not `.github/`, `UpdatePod.Domain.Tests/`, or `UpdatePod.sln`; `COPY . .` still sends them to the builder.
- The worker has no health endpoint/`HEALTHCHECK`; if deployed to Kubernetes, add exec/http liveness/readiness probes or a minimal health endpoint.

### L8. `KubernetesClient` is four majors behind

`KubernetesClient` is pinned to `17.0.14`, which contains the CVE-2025-9708 fix, but the latest major is `21.0.8`. Plan a separate upgrade and verify manifest/patch compatibility; the calls used here are small, so the migration should be manageable.

### L9. No `LICENSE` file

Add a license if the repository or the published image is meant to be reused publicly.

---

## Suggested order of work

1. **H1** (stop the plaintext credential fallback) and **H2** (keep the worker alive).
2. **M2/M3** (own the HTTP client/headers per request) and **M10** (Kubernetes retries).
3. **M4/M5/M6** (pod selection and image-reference parsing) plus deterministic tests (**M8**).
4. **M9**, **M11**, and the Low items as hardening/documentation work.

## Resolved in this PR

- **M1** — `HttpClientUtils.CreateTimeoutTokenSource` returns a disposable linked source; both registry strategies dispose it with `using`; non-positive timeouts are rejected. Tests cover timeout, parent cancellation, and invalid values.
- **M7** — Environment parsing is centralized in `EnvironmentConfiguration`; `PodUpdateOptions.FromEnvironment` and `ImageOperationData.FromEnvironment` validate required values, positive integers, and booleans before the host starts. Tests cover defaults, trimming, and invalid values.

## Already addressed during the .NET 11 upgrade

For context, these were fixed in the upgrade PR:

- All three projects moved from `net9.0` to `net11.0`.
- `Microsoft.Extensions.*` packages updated to `11.0.0`; the in-box `Microsoft.Extensions.Logging.Abstractions` reference was removed (NU1510) and documented.
- Dockerfile updated to `runtime:11.0` / `sdk:11.0`; the duplicate `-a -a` flag and the missing `TARGETARCH` declaration were fixed.
- Docker Hub invalid-token and token-success tests were made deterministic with fake handlers.
- CI was added: build/test, multi-arch Docker build, and GHCR publishing on `main` and `v*` tags.
- `KubernetesClient` was patched from `17.0.4` to `17.0.14` for CVE-2025-9708.
