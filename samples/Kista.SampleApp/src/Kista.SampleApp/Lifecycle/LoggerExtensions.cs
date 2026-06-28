using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

namespace Kista.SampleApp.Lifecycle {
    [ExcludeFromCodeCoverage]
    static partial class LoggerExtensions {
        [LoggerMessage(40000, LogLevel.Information, "Checking if {EntityType} repository exists")]
        public static partial void LogCheckingRepositoryExists(this ILogger logger, string entityType);

        [LoggerMessage(40001, LogLevel.Information, "Creating {EntityType} repository")]
        public static partial void LogCreatingRepository(this ILogger logger, string entityType);

        [LoggerMessage(40002, LogLevel.Information, "Dropping {EntityType} repository")]
        public static partial void LogDroppingRepository(this ILogger logger, string entityType);

        [LoggerMessage(40003, LogLevel.Information, "Seeding {EntityType} repository")]
        public static partial void LogSeedingRepository(this ILogger logger, string entityType);

        [LoggerMessage(40004, LogLevel.Information, "Seeded {Count} contacts")]
        public static partial void LogSeededCount(this ILogger logger, int count);

        [LoggerMessage(40005, LogLevel.Information, "No seed data available for {EntityType} repository")]
        public static partial void LogNoSeedData(this ILogger logger, string entityType);
    }
}
