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

namespace Kista.HealthChecks;

/// <summary>
/// Configuration options for repository health checks.
/// </summary>
public class RepositoryHealthCheckOptions {
    /// <summary>
    /// Default timeout for all health checks.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Default failure status to report when a health check fails.
    /// </summary>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Degraded;
    
    /// <summary>
    /// Default tags applied to all repository health checks.
    /// </summary>
    public string[] Tags { get; set; } = ["kista", "repository"];
    
    /// <summary>
    /// Startup validation mode (None, Warning, FailFast).
    /// </summary>
    public StartupValidationMode StartupValidationMode { get; set; } = StartupValidationMode.None;
    
    /// <summary>
    /// Naming configuration for health checks.
    /// </summary>
    public RepositoryHealthCheckNameOptions Naming { get; set; } = new();
    
    /// <summary>
    /// Per-repository configuration overrides.
    /// Key: Entity type, Value: Configuration action
    /// </summary>
    public Dictionary<Type, Action<RepositoryHealthCheckOptions>> PerRepositoryConfig { get; } = new();
    
    /// <summary>
    /// Excluded repository types (won't have health checks registered).
    /// </summary>
    public HashSet<Type> ExcludedRepositoryTypes { get; } = new();
    
    /// <summary>
    /// Configures options for a specific repository entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="configure">The configuration action.</param>
    public void ConfigureRepository<TEntity>(Action<RepositoryHealthCheckOptions> configure) {
        PerRepositoryConfig[typeof(TEntity)] = configure;
    }
}
