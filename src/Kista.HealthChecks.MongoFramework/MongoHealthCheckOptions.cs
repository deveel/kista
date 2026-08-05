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
using MongoDB.Bson;
using MongoDB.Driver;

namespace Kista.HealthChecks;

/// <summary>
/// Configuration options for MongoDB health checks.
/// </summary>
public class MongoHealthCheckOptions {
    /// <summary>
    /// Timeout for the health check.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Timeout for the ping command.
    /// </summary>
    public TimeSpan PingTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The duration for which a cached health-check result is reused before
    /// hitting the database again. Defaults to 5 seconds.
    /// </summary>
    /// <remarks>
    /// Orchestrators (Kubernetes, App Service, Docker) typically probe
    /// <c>/health</c> every 5–10s, and on a rolling restart they probe all
    /// replicas simultaneously. Without caching, each probe runs a
    /// <c>ping</c> command against MongoDB. This cache coalesces concurrent
    /// probes and prevents thundering-herd DB load during deploys.
    /// </remarks>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Tags to apply to the health check.
    /// </summary>
    public string[] Tags { get; set; } = ["kista", "mongodb", "repository"];
    
    /// <summary>
    /// Failure status to report when health check fails.
    /// </summary>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Degraded;
}
