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

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kista.HealthChecks;

/// <summary>
/// Extension methods for mapping Kista repository health check endpoints.
/// </summary>
public static class HealthCheckEndpointExtensions {
    /// <summary>
    /// Adds Kista repository health check endpoint with sensible defaults.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the health check endpoint.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The endpoint convention builder for further configuration.</returns>
    public static IEndpointConventionBuilder MapRepositoryHealthChecks(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/health",
        Action<RepositoryHealthCheckEndpointOptions>? configure = null) {
        
        var options = new RepositoryHealthCheckEndpointOptions();
        configure?.Invoke(options);
        
        var healthCheckOptions = new HealthCheckOptions {
            ResponseWriter = options.ResponseType == HealthCheckResponseFormat.Json
                ? JsonResponseWriter
                : TextResponseWriter,
            ResultStatusCodes = {
                [HealthStatus.Healthy] = options.SuccessStatusCode,
                [HealthStatus.Degraded] = options.DegradedStatusCode,
                [HealthStatus.Unhealthy] = options.UnhealthyStatusCode
            },
            AllowCachingResponses = options.AllowCaching,
            Predicate = options.TagFilter != null
                ? check => options.TagFilter(check.Tags)
                : null
        };
        
        return endpoints.MapHealthChecks(pattern, healthCheckOptions);
    }
    
    private static Task JsonResponseWriter(HttpContext context, HealthReport healthReport) {
        context.Response.ContentType = "application/json; charset=utf-8";
        
        var options = new JsonWriterOptions { Indented = true };
        
        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options)) {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("results");
            
            foreach (var healthReportEntry in healthReport.Entries) {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("description", healthReportEntry.Value.Description);
                jsonWriter.WriteStartObject("data");
                
                foreach (var item in healthReportEntry.Value.Data) {
                    jsonWriter.WritePropertyName(item.Key);
                    JsonSerializer.Serialize(jsonWriter, item.Value,
                        item.Value?.GetType() ?? typeof(object));
                }
                
                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }
            
            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }
        
        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
    
    private static Task TextResponseWriter(HttpContext context, HealthReport healthReport) {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(healthReport.Status.ToString());
    }
}
