using Microsoft.Extensions.Logging;

namespace Deveel.Data {
	/// <summary>
	/// Extension methods for logging repository lifecycle events.
	/// </summary>
	static partial class LifecycleLoggerExtensions {
		/// <summary>
		/// Logs that a lifecycle handler is being resolved for an entity type.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.ResolvingHandler, Level = LogLevel.Trace,
			Message = "Resolving lifecycle handler for entity of type {EntityType}")]
		public static partial void LogResolvingHandler(this ILogger logger, string entityType);

		/// <summary>
		/// Logs that a lifecycle handler was successfully resolved.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="handlerType">The handler type name.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.HandlerResolved, Level = LogLevel.Trace,
			Message = "Resolved lifecycle handler of type '{HandlerType}' for entity '{EntityType}'")]
		public static partial void LogHandlerResolved(this ILogger logger, string handlerType, string entityType);

		/// <summary>
		/// Logs that the system is falling back to <see cref="IControllableRepository"/>.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.FallingBack, Level = LogLevel.Trace,
			Message = "Falling back to IControllableRepository for entity '{EntityType}'")]
		public static partial void LogFallingBackToControllable(this ILogger logger, string entityType);

		/// <summary>
		/// Logs that no lifecycle handler was found for an entity type.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.NoHandlerFound, Level = LogLevel.Trace,
			Message = "No lifecycle handler found for entity '{EntityType}'")]
		public static partial void LogNoHandlerFound(this ILogger logger, string entityType);

		/// <summary>
		/// Logs that an existing repository is being deleted before recreation.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.DeletingExisting, Level = LogLevel.Trace,
			Message = "The repository already exists and the orchestrator is deleting it first")]
		public static partial void TraceDeletingExisting(this ILogger logger);

		/// <summary>
		/// Logs that an existing repository is being skipped during creation.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.SkippingExisting, Level = LogLevel.Warning,
			Message = "The repository already exists and the orchestrator is not deleting it")]
		public static partial void WarnSkippingExisting(this ILogger logger);

		/// <summary>
		/// Logs that a repository is being created.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.Creating, Level = LogLevel.Trace,
			Message = "Creating the repository")]
		public static partial void TraceCreatingRepository(this ILogger logger);

		/// <summary>
		/// Logs that a repository was successfully created.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.Created, Level = LogLevel.Trace,
			Message = "Repository created")]
		public static partial void TraceRepositoryCreated(this ILogger logger);

		/// <summary>
		/// Logs that repository deletion is skipped because it does not exist.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.NotExistsSkipping, Level = LogLevel.Trace,
			Message = "The repository does not exist: it will not be deleted")]
		public static partial void TraceNotExistsSkipping(this ILogger logger);

		/// <summary>
		/// Logs that a repository is being dropped.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.Dropping, Level = LogLevel.Trace,
			Message = "Dropping the repository")]
		public static partial void TraceDroppingRepository(this ILogger logger);

		/// <summary>
		/// Logs that a repository was successfully dropped.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.Dropped, Level = LogLevel.Trace,
			Message = "The repository was dropped")]
		public static partial void TraceRepositoryDropped(this ILogger logger);

		/// <summary>
		/// Logs that seeding is skipped because the repository already exists.
		/// </summary>
		/// <param name="logger">The logger.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.SkippingSeed, Level = LogLevel.Warning,
			Message = "The repository already exists: skipping seed operation")]
		public static partial void WarnSkippingSeed(this ILogger logger);

		/// <summary>
		/// Logs that a repository is being seeded.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.Seeding, Level = LogLevel.Trace,
			Message = "Seeding the repository for entity '{EntityType}'")]
		public static partial void TraceSeedingRepository(this ILogger logger, string entityType);

		/// <summary>
		/// Logs that a repository was successfully seeded.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.SeedCompleted, Level = LogLevel.Trace,
			Message = "Repository seeded for entity '{EntityType}'")]
		public static partial void TraceRepositorySeeded(this ILogger logger, string entityType);

		/// <summary>
		/// Logs that no seed data is available for an entity type.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="entityType">The entity type name.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.NoSeedData, Level = LogLevel.Trace,
			Message = "No seed data available for entity '{EntityType}'")]
		public static partial void TraceNoSeedData(this ILogger logger, string entityType);

		/// <summary>
		/// Logs a not supported error during a repository lifecycle operation.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="exception">The exception.</param>
		/// <param name="operation">The operation being performed.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.NotSupportedError, Level = LogLevel.Error,
			Message = "Not Supported Error while {Operation} the repository")]
		public static partial void LogNotSupportedError(this ILogger logger, Exception exception, string operation);

		/// <summary>
		/// Logs a repository-specific error during a lifecycle operation.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="exception">The exception.</param>
		/// <param name="operation">The operation being performed.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.RepositoryError, Level = LogLevel.Error,
			Message = "Repository Error while {Operation} the repository")]
		public static partial void LogRepositoryError(this ILogger logger, Exception exception, string operation);

		/// <summary>
		/// Logs a general error during a repository lifecycle operation.
		/// </summary>
		/// <param name="logger">The logger.</param>
		/// <param name="exception">The exception.</param>
		/// <param name="operation">The operation being performed.</param>
		[LoggerMessage(EventId = LifecycleLogEventIds.GeneralError, Level = LogLevel.Error,
			Message = "Error while {Operation} the repository")]
		public static partial void LogGeneralError(this ILogger logger, Exception exception, string operation);
	}
}
