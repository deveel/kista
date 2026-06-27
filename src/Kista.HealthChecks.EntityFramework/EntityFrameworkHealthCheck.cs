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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks;

/// <summary>
/// Health check implementation for Entity Framework repositories.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public class EntityFrameworkHealthCheck<TEntity, TKey> : RepositoryHealthCheckBase<TEntity, TKey>
    where TEntity : class {
    
    private readonly EntityFrameworkHealthCheckOptions _options;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkHealthCheck{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="options">The health check options.</param>
    public EntityFrameworkHealthCheck(IOptions<EntityFrameworkHealthCheckOptions> options) {
        _options = options?.Value ?? new EntityFrameworkHealthCheckOptions();
    }
    
    /// <inheritdoc/>
    public override string DriverType => "EntityFramework";
    
    /// <inheritdoc/>
    protected override async ValueTask<HealthCheckResult> CheckHealthAsyncCore(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken) {
        
        var context = serviceProvider.GetRequiredService<DbContext>();
        
        try {
            // Test connection
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect) {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    exception: null,
                    data: CreateDiagnosticData(
                        KeyValuePair.Create<string, object?>("DbContextType", context.GetType().FullName),
                        KeyValuePair.Create<string, object?>("ConnectionState", "Disconnected")));
            }
            
            // Optional: Run a lightweight query
            if (_options.TestQuery) {
                var exists = await context.Set<TEntity>().AnyAsync(cancellationToken);
                return HealthCheckResult.Healthy(
                    "Database connection successful",
                    data: CreateDiagnosticData(
                        KeyValuePair.Create<string, object?>("DbContextType", context.GetType().FullName),
                        KeyValuePair.Create<string, object?>("EntityExists", exists),
                        KeyValuePair.Create<string, object?>("ResponseType", "Healthy")));
            }
            
            return HealthCheckResult.Healthy(
                "Database connection successful",
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("DbContextType", context.GetType().FullName),
                    KeyValuePair.Create<string, object?>("ResponseType", "Healthy")));
        }
        catch (DbUpdateException ex) {
            return HealthCheckResult.Unhealthy(
                $"Database update failed: {ex.Message}",
                exception: ex,
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("DbContextType", context.GetType().FullName),
                    KeyValuePair.Create<string, object?>("ExceptionType", ex.GetType().FullName)));
        }
        catch (InvalidOperationException ex) {
            return HealthCheckResult.Unhealthy(
                $"Database operation invalid: {ex.Message}",
                exception: ex,
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("DbContextType", context.GetType().FullName),
                    KeyValuePair.Create<string, object?>("ExceptionType", ex.GetType().FullName)));
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            return HealthCheckResult.Unhealthy(
                $"Database connection failed: {ex.Message}",
                exception: ex,
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("DbContextType", context.GetType().FullName),
                    KeyValuePair.Create<string, object?>("ExceptionType", ex.GetType().FullName)));
        }
    }
}
