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
