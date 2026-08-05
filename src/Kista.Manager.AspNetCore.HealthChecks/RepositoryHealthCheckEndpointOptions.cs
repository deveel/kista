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

namespace Kista.HealthChecks;

/// <summary>
/// Configuration options for repository health check endpoints.
/// </summary>
public class RepositoryHealthCheckEndpointOptions {
    /// <summary>
    /// The response format (Text or Json).
    /// Default: Json
    /// </summary>
    public HealthCheckResponseFormat ResponseType { get; set; } = HealthCheckResponseFormat.Json;
    
    /// <summary>
    /// HTTP status code for healthy responses.
    /// Default: 200 OK
    /// </summary>
    public int SuccessStatusCode { get; set; } = 200;
    
    /// <summary>
    /// HTTP status code for degraded responses.
    /// Default: 200 OK
    /// </summary>
    public int DegradedStatusCode { get; set; } = 200;
    
    /// <summary>
    /// HTTP status code for unhealthy responses.
    /// Default: 503 Service Unavailable
    /// </summary>
    public int UnhealthyStatusCode { get; set; } = 503;
    
    /// <summary>
    /// Whether to allow caching of health check responses.
    /// Default: false
    /// </summary>
    public bool AllowCaching { get; set; } = false;
    
    /// <summary>
    /// Optional filter for health check tags.
    /// If null, all health checks are included.
    /// </summary>
    public Func<IEnumerable<string>, bool>? TagFilter { get; set; }

    /// <summary>
    /// Whether to include exception details (descriptions and diagnostic data)
    /// in the health check response. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// When <c>false</c>, the JSON response includes only the aggregate
    /// <c>status</c> and the entry names — never exception messages or
    /// diagnostic data that could disclose infrastructure details (connection
    /// strings, table names, SQL/Mongo error text, file paths). Set to
    /// <c>true</c> in development or behind a trusted gateway to aid debugging.
    /// </remarks>
    public bool IncludeExceptionDetails { get; set; } = false;
}
