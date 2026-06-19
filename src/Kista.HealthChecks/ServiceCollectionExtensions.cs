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

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Kista.HealthChecks.Internal;

namespace Kista.HealthChecks;

/// <summary>
/// Extension methods for configuring Kista repository health checks.
/// </summary>
public static partial class ServiceCollectionExtensions {
    /// <summary>
    /// Registers health checks for all Kista repositories registered via AddRepositoryContext().
    /// This method can be called explicitly or will be called implicitly at the end of
    /// AddRepositoryContext() if not already called.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddKistaRepositories(
        this IHealthChecksBuilder builder,
        Action<RepositoryHealthCheckOptions>? configure = null) {
        
        var services = builder.Services;
        
        // Prevent double-registration
        if (services.Any(d => d.ServiceType == typeof(Internal.IKistaHealthChecksRegistered)))
            return builder;
        
        services.AddSingleton<Internal.IKistaHealthChecksRegistered>(
            new Internal.KistaHealthChecksRegisteredMarker());
        
        var options = new RepositoryHealthCheckOptions();
        configure?.Invoke(options);
        
        // Register options
        services.AddSingleton(options);
        
        // Get registry of repositories registered via AddRepositoryContext()
        var registry = services
            .FirstOrDefault(d => d.ServiceType == typeof(Internal.IRepositoryContextRegistry))
            ?.ImplementationInstance as Internal.RepositoryContextRegistry;
        
        if (registry == null) {
            // No repositories registered via context builder, skip registration
            return builder;
        }
        
        // Get driver markers
        var healthCheckMarkers = services
            .Where(d => d.ServiceType == typeof(Internal.IHealthCheckMarker))
            .Select(d => d.ImplementationInstance as Internal.IHealthCheckMarker)
            .Where(m => m != null)
            .ToList();
        
        foreach (var repoRegistration in registry.RegisteredRepositories) {
            var repoType = repoRegistration.RepositoryType;
            var entityType = repoRegistration.EntityType;
            var keyType = repoRegistration.KeyType;
            
            // Check exclusions
            if (options.ExcludedRepositoryTypes.Contains(repoType))
                continue;
            
            // Apply per-repository configuration
            var repoOptions = CloneOptions(options);
            if (options.PerRepositoryConfig.TryGetValue(entityType, out var configAction))
                configAction(repoOptions);
            
            // Determine driver type and register appropriate health check
            var driverType = DetermineDriverType(repoType, healthCheckMarkers);
            if (driverType == null)
                continue; // Skip if no driver marker found
            
            var healthCheckName = GenerateHealthCheckName(repoRegistration, repoOptions.Naming, driverType);
            
            RegisterHealthCheckForDriver(
                builder, driverType, entityType, keyType, repoType, 
                healthCheckName, repoOptions);
        }
        
        // Register startup validator if enabled
        if (options.StartupValidationMode != StartupValidationMode.None) {
            services.AddHostedService<RepositoryHealthCheckStartupValidator>();
        }
        
        return builder;
    }
    
    private static RepositoryHealthCheckOptions CloneOptions(RepositoryHealthCheckOptions source) {
        return new RepositoryHealthCheckOptions {
            Timeout = source.Timeout,
            FailureStatus = source.FailureStatus,
            Tags = source.Tags.ToArray(),
            StartupValidationMode = source.StartupValidationMode,
            Naming = new RepositoryHealthCheckNameOptions {
                Template = source.Naming.Template,
                NameGenerator = source.Naming.NameGenerator
            }
        };
    }
    
    private static string? DetermineDriverType(Type repoType, List<Internal.IHealthCheckMarker?> markers) {
        // Check which driver markers are present
        var hasEf = markers.Any(m => m?.DriverType == "EntityFramework");
        var hasMongo = markers.Any(m => m?.DriverType == "MongoDB");
        var hasInMemory = markers.Any(m => m?.DriverType == "InMemory");
        
        // Determine driver from repository type
        var repoTypeName = repoType.FullName ?? "";
        
        if (repoTypeName.Contains("EntityRepository") || repoTypeName.Contains("EntityFramework"))
            return hasEf ? "EntityFramework" : null;
        
        if (repoTypeName.Contains("MongoRepository"))
            return hasMongo ? "MongoDB" : null;
        
        if (repoTypeName.Contains("InMemoryRepository"))
            return hasInMemory ? "InMemory" : null;
        
        return null;
    }
    
    private static string GenerateHealthCheckName(
        RepositoryRegistration registration,
        RepositoryHealthCheckNameOptions namingOptions,
        string driverType) {
        
        // Use custom name generator if provided
        if (namingOptions.NameGenerator != null)
            return namingOptions.NameGenerator(registration.RepositoryType);
        
        // Use template
        var template = namingOptions.Template;
        return template
            .Replace("{Driver}", driverType)
            .Replace("{EntityType}", registration.EntityType.Name)
            .Replace("{RepositoryType}", registration.RepositoryType.Name);
    }
    
    private static void RegisterHealthCheckForDriver(
        IHealthChecksBuilder builder,
        string driverType,
        Type entityType,
        Type keyType,
        Type repoType,
        string healthCheckName,
        RepositoryHealthCheckOptions options) {
        
        // Driver-specific health checks are registered by extension methods in driver packages
        // This method is a placeholder that can be extended via partial methods or events
        // For now, we skip registration here to avoid circular dependencies
    }
}

/// <summary>
/// A delegated health check that wraps a Func.
/// </summary>
internal sealed class DelegatedHealthCheck : IHealthCheck {
    private readonly Func<HealthCheckContext, CancellationToken, Task<HealthCheckResult>> _checkFunc;
    
    public DelegatedHealthCheck(Func<HealthCheckContext, CancellationToken, Task<HealthCheckResult>> checkFunc) {
        _checkFunc = checkFunc;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) {
        return _checkFunc(context, cancellationToken);
    }
}
