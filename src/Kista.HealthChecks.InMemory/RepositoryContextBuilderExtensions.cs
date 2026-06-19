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

using Kista.HealthChecks;
using Kista.HealthChecks.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks;

/// <summary>
/// Extension methods for configuring In-Memory health checks on repository builders.
/// </summary>
public static class RepositoryContextBuilderExtensions {
    /// <summary>
    /// Enables health checks for In-Memory repositories.
    /// Configuration is stored and applied when AddKistaRepositories() is called.
    /// </summary>
    /// <param name="builder">The In-Memory repository builder.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    public static InMemoryRepositoryBuilder WithHealthChecks(
        this InMemoryRepositoryBuilder builder,
        Action<InMemoryHealthCheckOptions>? configure = null) {
        
        var options = new InMemoryHealthCheckOptions();
        configure?.Invoke(options);
        
        // Store configuration using Options pattern
        builder.Services.Configure<InMemoryHealthCheckOptions>(
            Options.DefaultName,
            opts => {
                opts.Timeout = options.Timeout;
                opts.Tags = options.Tags;
                opts.FailureStatus = options.FailureStatus;
            });
        
        // Mark that health checks are enabled for this driver
        builder.Services.AddSingleton<IHealthCheckMarker>(
            new InMemoryHealthCheckMarker());
        
        return builder;
    }
}
