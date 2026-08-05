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

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kista.HealthChecks;

/// <summary>
/// Extension methods for mapping Kista repository health check endpoints.
/// </summary>
public static class HealthCheckEndpointExtensions {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
                ? (context, healthReport) => WriteJsonResponse(context, healthReport, options)
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

    private static async Task WriteJsonResponse(
        HttpContext context,
        HealthReport healthReport,
        RepositoryHealthCheckEndpointOptions options) {
        context.Response.ContentType = "application/json; charset=utf-8";

        var writerOptions = new JsonWriterOptions { Indented = true };

        await using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, writerOptions)) {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("results");

            foreach (var entry in healthReport.Entries) {
                jsonWriter.WriteStartObject(entry.Key);
                jsonWriter.WriteString("status", entry.Value.Status.ToString());

                if (options.IncludeExceptionDetails) {
                    jsonWriter.WriteString("description", entry.Value.Description);
                    jsonWriter.WriteStartObject("data");

                    foreach (var item in entry.Value.Data) {
                        jsonWriter.WritePropertyName(item.Key);
                        JsonSerializer.Serialize(jsonWriter, item.Value, typeof(object), JsonOptions);
                    }

                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        // Write the buffered JSON to the response stream asynchronously
        // (Kestrel disallows synchronous IO by default).
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(context.Response.Body, 8192, context.RequestAborted).ConfigureAwait(false);
    }
    
    private static Task TextResponseWriter(HttpContext context, HealthReport healthReport) {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(healthReport.Status.ToString());
    }
}
