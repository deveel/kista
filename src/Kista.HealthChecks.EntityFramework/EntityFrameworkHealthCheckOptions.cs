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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks;

/// <summary>
/// Configuration options for Entity Framework health checks.
/// </summary>
public class EntityFrameworkHealthCheckOptions {
    /// <summary>
    /// Timeout for the health check.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Whether to run a test query in addition to connection check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the health check runs a <c>SELECT EXISTS</c> against
    /// the entity table on every probe. This doubles the DB load under probe
    /// pressure (Kubernetes/App Service orchestrators typically probe every
    /// 5–10s). Leave <c>false</c> for liveness probes; enable only for
    /// readiness probes that must verify schema availability.
    /// </remarks>
    public bool TestQuery { get; set; } = false;

    /// <summary>
    /// The duration for which a cached health-check result is reused before
    /// hitting the database again. Defaults to 5 seconds.
    /// </summary>
    /// <remarks>
    /// Orchestrators (Kubernetes, App Service, Docker) typically probe
    /// <c>/health</c> every 5–10s, and on a rolling restart they probe all
    /// replicas simultaneously. Without caching, each probe opens a physical
    /// DB connection. This cache coalesces concurrent probes and prevents
    /// thundering-herd DB load during deploys.
    /// </remarks>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Tags to apply to the health check.
    /// </summary>
    public string[] Tags { get; set; } = ["kista", "entityframework", "repository"];
    
    /// <summary>
    /// Failure status to report when health check fails.
    /// </summary>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Degraded;
}
