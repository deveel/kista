# Migrating from 1.7.5 to 1.7.6

The **1.7.6 — Security & Performance Hardening** patch release closes a 40-finding security and performance review of the solution (13 security, 27 performance) and ships **five consumer-visible breaking changes**, all of which fail closed by default. Legitimate filters and read paths are unaffected in the vast majority of applications; the breaking surface targets unsafe defaults that previously allowed owner-scope impersonation, write-path IDOR, remote-code execution via Dynamic LINQ, unbounded memory growth, and exception-message leakage to health endpoints.

For the prior 1.7.3 → 1.7.4 migration (cache alignment to the operation pipeline), see [Migrating from 1.7.3](migrating-from-1.7.3.md).

## Summary of breaking changes

| # | Area | Change | Fail mode |
|---|------|--------|-----------|
| 1 | `Kista.Owners` | `AddHttpUserAccessor<TKey>` default chain is claim-only | Query-string/route fallbacks removed (opt-in) |
| 2 | `Kista.Owners` | Write paths verify ownership before forwarding | `UnauthorizedAccessException` on mismatch |
| 3 | `Kista.Owners` | `UserScopingOptions.ThrowWhenUserNotSet` defaults to `true` | `InvalidOperationException` when no user resolved |
| 4 | `Kista.DynamicLinq` | `KistaParsingConfig` blocks `new` and fully-qualified casts | Parse-time exception on blocked expressions |
| 5 | `Kista.EntityFramework` | EF read paths use `AsNoTracking()` by default | Returned entities are not tracked; set `TrackEntities=true` to mutate |
| 6 | Health checks | `Description`/`Data` gated behind `IncludeExceptionDetails` | Production emits aggregate status + entry names only |

> **Note:** Changes 5 and 6 are breaking at the behavior level, not the API surface — no public type or member is removed or renamed. Changes 1–4 are default-flip and contract changes that may require registration or filter-audit updates.

## 1. Owner-scope impersonation: claim-only default chain

**Before:** `AddHttpUserAccessor<TKey>()` registered a default resolution chain of `sub` claim → `?user_id=` query string → `userId` route value. An unauthenticated request with no `sub` claim fell through to client-controlled query-string or route values, allowing any caller to impersonate another user by appending `?user_id=<victim>`.

**After:** The default chain is **claim-only** (`"sub"`). The query-string and route fallback strategies are opt-in and must only be enabled behind a trusted gateway that has already authenticated the caller.

### Do I need to change my code?

- **If you rely on the claim-based chain (the secure default):** no change needed.
- **If you relied on the query-string or route fallbacks** (e.g. an internal service that passes the user id through the query string after gateway auth), explicitly opt in:

  ```csharp
  services.AddHttpUserAccessor<Guid>(b => b
      .AddClaim()
      .AddQueryString()
      .AddRoute());
  ```

  Only enable `AddQueryString()` / `AddRoute()` when the upstream gateway guarantees the value is authenticated and trusted — otherwise the impersonation vector reopens.

## 2. Write-path IDOR: ownership verification

**Before:** `UpdateAsync`, `RemoveAsync`, and `RemoveRangeAsync` on `UserScopedRepositoryDecorator` forwarded directly to the inner repository with no ownership check. An attacker who knew another owner's entity key could update or delete that entity.

**After:** These methods now fetch the persisted entity by key and verify `entity.Owner == currentUserId` before forwarding. An `UnauthorizedAccessException` is thrown on mismatch or when the entity cannot be found.

### Do I need to change my code?

- **No** for correct applications — only the caller who owns the entity can mutate it.
- If a test or service was silently relying on the bypass (e.g. an admin path that wrote across owners), it now throws. Route such operations through a non-user-scoped repository or an explicit elevated-privilege path.

## 3. `ThrowWhenUserNotSet` default flip

**Before:** `UserScopingOptions.ThrowWhenUserNotSet` defaulted to `false` (fail-open). When no user identity was resolvable, operations silently returned empty results. The XML doc claimed the default was `true` while the code defaulted to `false`.

**After:** The default is `true` (fail-closed). When no user identity is resolvable, operations throw `InvalidOperationException`. The XML doc now matches the code.

### Do I need to change my code?

- **No** for authenticated applications — a resolvable user was always present.
- To restore fail-open behavior (e.g. for a public read path that should return empty results when no user is present), set the option explicitly:

  ```csharp
  services.Configure<UserScopingOptions>(o => o.ThrowWhenUserNotSet = false);
  ```

## 4. Dynamic LINQ: `KistaParsingConfig`

**Before:** `FilterExpression.AsLambda` and `Compile` used `ParsingConfig.Default`, which permits the `new` operator and fully-qualified type casts. Since `DynamicLinqFilter` expression strings are commonly received from API clients or configuration, an attacker could craft expressions like `new System.Diagnostics.Process()` leading to remote code execution.

**After:** All parse sites use `KistaParsingConfig`, which sets `DisallowNewKeyword = true` and `SupportCastingToFullyQualifiedTypeAsString = false`. See [Dynamic LINQ Security](filtering/dynamic-linq-security.md).

### Do I need to change my code?

- **No** for legitimate filters — property access, comparisons, boolean logic, and `Contains` / `StartsWith` are unaffected.
- If a filter expression used `new` or a fully-qualified cast (neither is a legitimate query pattern), it now throws at parse time. Rewrite the expression to use property access only, or construct the expression server-side from validated parameters.

## 5. EF Core read paths: `AsNoTracking()` by default

**Before:** `FindFirstAsync`, `FindAllAsync`, and `GetPageAsync` materialized fully-tracked entities. Every row was registered in the `ChangeTracker`, growing memory linearly with result-set size and slowing `SaveChangesAsync` on the same `DbContext`.

**After:** These read paths use `AsNoTracking()` by default. Returned entities are detached; mutations to them are not persisted on the next `SaveChangesAsync`.

### Do I need to change my code?

- **No** for read-only paths and for write paths that call `UpdateAsync` explicitly (the manager attaches and stamps the entity).
- If you mutate a returned entity and rely on the change tracker to persist the change implicitly, set `TrackEntities = true` on the `IQueryOptions` passed to the read call:

  ```csharp
  var opts = QueryOptions.Empty.WithTracking();
  var entity = await manager.FindFirstAsync(filter, opts, ct);
  entity.Status = "approved";
  await manager.UpdateAsync(entity, ct); // explicit Update is still the recommended path
  ```

  The recommended pattern remains: read with `AsNoTracking`, then call `UpdateAsync` explicitly — this is unambiguous and works regardless of the tracking setting.

## 6. Health-check JSON: exception details gated

**Before:** The health-check endpoint serialized raw `ex.Message` of every failing repository health check into the HTTP response body, disclosing infrastructure details (connection-string hostnames, table names, SQL/Mongo error text, file paths). The `Data` dictionary was serialized with runtime-type polymorphism.

**After:** `Description` and `Data` serialization is gated behind `IHostEnvironment.IsDevelopment()` / `HealthCheckEndpointOptions.IncludeExceptionDetails` (default `false`). Production emits only the aggregate `Status` and entry names. The JSON writer uses a cached `static readonly JsonSerializerOptions` and `typeof(object)` (no runtime-type polymorphism), streamed directly to the response body.

### Do I need to change my code?

- **No** for production — the default is secure.
- To restore diagnostic detail in a trusted environment, enable it explicitly:

  ```csharp
  services.Configure<HealthCheckEndpointOptions>(o => o.IncludeExceptionDetails = true);
  ```

  Never enable `IncludeExceptionDetails` in production or on a public endpoint.

## Other behavior and performance changes

These are non-breaking but worth noting:

- **`EntityMemoryCache` defaults to a 5-minute expiration** (was indefinite), aligning with `EntityDistributedCache` and `EntityFusionCache`.
- **`DefaultEntityCacheKeyGenerator`** validates length, URL-encodes the key segment, and prefixes with the entity type name to prevent cache-key injection. If you registered a custom `IEntityCacheKeyGenerator`, it is unchanged — the default generator is only used when no custom one is registered.
- **`OperationErrorFactory.CreateError`** no longer surfaces raw `exception.Message` for non-`OperationException` cases; the error code conveys meaning. `OperationException.Message` (application-controlled) is still honored.
- **`BoundedCache`** replaces a global `SemaphoreSlim(1,1)` with `ReaderWriterLockSlim` — readers no longer block each other on Dynamic LINQ filter parses.
- **Mongo `CountAsync(IQueryable)`** is now async with cancellation (was sync `queryable.Count()`).
- **`EfQueryNormalizer`** emits a bare `LIKE` instead of wrapping in null-checks that could prevent the provider from using the index on the LIKE prefix.
- **19 sync-over-async wrappers** in `RepositoryExtensions` are marked `[Obsolete("Use the async overload. Sync-over-async can cause threadpool starvation.")]`.
- **`System.Linq.Dynamic.Core`** bumped from 1.7.2 to 1.7.3.

## Reference

- [Dynamic LINQ Security](filtering/dynamic-linq-security.md) — `KistaParsingConfig` and application-level allow-list guidance
- [User Entities](user-entities/user-identifier-resolution.md) — `IUserAccessor` chain and the `AddHttpUserAccessor` registration
- [Health Checks](health-checks/overview.md) — probe configuration, `CacheDuration`, and `IncludeExceptionDetails`
- [Migrating from 1.7.3](migrating-from-1.7.3.md) — the prior 1.7.3 → 1.7.4 migration (cache alignment to the operation pipeline)