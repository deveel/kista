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
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks;

/// <summary>
/// Health check implementation for In-Memory repositories.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public class InMemoryHealthCheck<TEntity, TKey> : RepositoryHealthCheckBase<TEntity, TKey>
    where TEntity : class {
    
    private readonly InMemoryHealthCheckOptions _options;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryHealthCheck{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="options">The health check options.</param>
    public InMemoryHealthCheck(IOptions<InMemoryHealthCheckOptions> options) {
        _options = options?.Value ?? new InMemoryHealthCheckOptions();
    }
    
    /// <inheritdoc/>
    public override string DriverType => "InMemory";
    
    /// <inheritdoc/>
    protected override ValueTask<HealthCheckResult> CheckHealthAsyncCore(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken) {
        
        // In-memory is always healthy if registered
        return new ValueTask<HealthCheckResult>(
            HealthCheckResult.Healthy(
                "In-memory repository is available",
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("ResponseType", "Healthy"))));
    }
}
