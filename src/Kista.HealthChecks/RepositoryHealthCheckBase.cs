// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kista.HealthChecks;

/// <summary>
/// Base class for repository health checks that provides common functionality
/// such as timeout handling and exception mapping.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public abstract class RepositoryHealthCheckBase<TEntity, TKey> : IRepositoryHealthCheck
    where TEntity : class {

    /// <summary>
    /// Serializes concurrent probes so that only one thread performs the
    /// underlying driver check while the others wait for the cached result.
    /// </summary>
    protected readonly SemaphoreSlim ProbeSemaphore = new(1, 1);

    /// <summary>
    /// The last probe result, reused while <see cref="CacheExpiry"/> is in
    /// the future to avoid hitting the data store on every probe.
    /// </summary>
    protected HealthCheckResult? CachedProbeResult;

    /// <summary>
    /// The instant at which <see cref="CachedProbeResult"/> becomes stale.
    /// </summary>
    protected DateTimeOffset CacheExpiry;

    /// <inheritdoc/>
    public Type RepositoryType => typeof(IRepository<TEntity, TKey>);
    
    /// <inheritdoc/>
    public abstract string DriverType { get; }
    
    /// <inheritdoc/>
    public async ValueTask<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken) {
        
        try {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(context.Registration.Timeout);
            
            return await CheckHealthAsyncCore(serviceProvider, cts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // User cancellation, not timeout
            throw;
        }
        catch (OperationCanceledException ex) {
            // Timeout
            return HealthCheckResult.Unhealthy(
                "Health check timed out",
                exception: ex,
                data: CreateDiagnosticData(KeyValuePair.Create<string, object?>("ErrorType", "Timeout")));
        }
        catch (SystemException ex) {
            return HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                exception: ex,
                data: CreateDiagnosticData(KeyValuePair.Create<string, object?>("ExceptionType", ex.GetType().FullName)));
        }
    }
    
    /// <summary>
    /// Performs the actual health check logic.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The health check result.</returns>
    protected abstract ValueTask<HealthCheckResult> CheckHealthAsyncCore(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes <paramref name="probe"/> with a short-lived in-process cache
    /// and a coalescing semaphore, so that concurrent probes share a single
    /// driver round-trip and repeated probes within <paramref name="cacheDuration"/>
    /// return the cached <see cref="HealthCheckResult"/> without touching the store.
    /// </summary>
    /// <param name="probe">
    /// A delegate that performs the actual driver-specific check and returns
    /// a fresh <see cref="HealthCheckResult"/>.
    /// </param>
    /// <param name="cacheDuration">
    /// How long a successful probe result is reused. Pass
    /// <see cref="TimeSpan.Zero"/> to disable caching (every probe hits the
    /// delegate).
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation. Cancellation is honoured before
    /// entering the semaphore and re-checked inside the critical section.
    /// </param>
    /// <returns>
    /// The cached <see cref="HealthCheckResult"/> if still valid, otherwise the
    /// result of <paramref name="probe"/>.
    /// </returns>
    /// <remarks>
    /// When <paramref name="cacheDuration"/> is <see cref="TimeSpan.Zero"/> the
    /// semaphore is still acquired so concurrent callers coalesce onto a single
    /// in-flight probe, but every caller observes a fresh result.
    /// </remarks>
    protected async ValueTask<HealthCheckResult> ExecuteCachedProbeAsync(
        Func<ValueTask<HealthCheckResult>> probe,
        TimeSpan cacheDuration,
        CancellationToken cancellationToken) {

        // Fast-path: if already cancelled, throw before entering the semaphore
        // to preserve the OperationCanceledException type contract.
        cancellationToken.ThrowIfCancellationRequested();

        // Return a cached result if still valid, to avoid hitting the store on every probe.
        if (cacheDuration > TimeSpan.Zero
            && CachedProbeResult is { } cached
            && DateTimeOffset.UtcNow < CacheExpiry)
            return cached;

        // Coalesce concurrent probes: only one thread runs the probe, others wait.
        await ProbeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            // Double-check after acquiring the lock — another thread may have refreshed.
            if (cacheDuration > TimeSpan.Zero
                && CachedProbeResult is { } refreshed
                && DateTimeOffset.UtcNow < CacheExpiry)
                return refreshed;

            var result = await probe().ConfigureAwait(false);

            if (cacheDuration > TimeSpan.Zero) {
                CachedProbeResult = result;
                CacheExpiry = DateTimeOffset.UtcNow + cacheDuration;
            }

            return result;
        } finally {
            ProbeSemaphore.Release();
        }
    }
    
    protected static Dictionary<string, object> CreateDiagnosticData(params KeyValuePair<string, object?>[] additionalData) {
        var data = new Dictionary<string, object> {
            ["EntityType"] = typeof(TEntity).Name,
            ["KeyType"] = typeof(TKey).Name,
            ["ResponseType"] = "Healthy"
        };
        
        foreach (var kvp in additionalData.Where(kvp => kvp.Value != null)) {
            data[kvp.Key] = kvp.Value!;
        }
        
        return data;
    }
}
