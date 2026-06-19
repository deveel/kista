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
/// The contract defining a health check for a Kista repository.
/// </summary>
public interface IRepositoryHealthCheck {
    /// <summary>
    /// Gets the type of repository being checked.
    /// </summary>
    Type RepositoryType { get; }
    
    /// <summary>
    /// Gets the driver type (e.g., "EntityFramework", "MongoDB", "InMemory").
    /// </summary>
    string DriverType { get; }
    
    /// <summary>
    /// Checks the health of the repository.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes with the health check result.</returns>
    ValueTask<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
