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

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Kista.HealthChecks {
    [ExcludeFromCodeCoverage]
    static partial class LoggerExtensions {
        [LoggerMessage(30000, LogLevel.Information, "Starting repository health check validation...")]
        public static partial void LogStartingValidation(this ILogger logger);

        [LoggerMessage(30001, LogLevel.Error, "Repository health checks failed at startup: {Status}")]
        public static partial void LogFailedStartup(this ILogger logger, string status);

        [LoggerMessage(30002, LogLevel.Warning, "Repository health checks failed at startup: {Status}")]
        public static partial void LogFailedWarning(this ILogger logger, string status);

        [LoggerMessage(30003, LogLevel.Warning, "Health check '{Name}' reported {Status}: {Description}")]
        public static partial void LogCheckFailed(this ILogger logger, string name, HealthStatus status, string? description);

        [LoggerMessage(30004, LogLevel.Information, "Repository health checks passed at startup.")]
        public static partial void LogPassedValidation(this ILogger logger);

        [LoggerMessage(30005, LogLevel.Error, "Repository health check validation failed with exception.")]
        public static partial void LogValidationException(this ILogger logger, Exception exception);
    }
}
