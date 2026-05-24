using Microsoft.Extensions.Logging;

namespace Deveel.Data {
	static partial class LifecycleLoggerExtensions {
		[LoggerMessage(EventId = LifecycleLogEventIds.ResolvingHandler, Level = LogLevel.Trace,
			Message = "Resolving lifecycle handler for entity of type {EntityType}")]
		public static partial void LogResolvingHandler(this ILogger logger, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.HandlerResolved, Level = LogLevel.Trace,
			Message = "Resolved lifecycle handler of type '{HandlerType}' for entity '{EntityType}'")]
		public static partial void LogHandlerResolved(this ILogger logger, string handlerType, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.FallingBack, Level = LogLevel.Trace,
			Message = "Falling back to IControllableRepository for entity '{EntityType}'")]
		public static partial void LogFallingBackToControllable(this ILogger logger, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.NoHandlerFound, Level = LogLevel.Trace,
			Message = "No lifecycle handler found for entity '{EntityType}'")]
		public static partial void LogNoHandlerFound(this ILogger logger, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.DeletingExisting, Level = LogLevel.Trace,
			Message = "The repository already exists and the orchestrator is deleting it first")]
		public static partial void TraceDeletingExisting(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.SkippingExisting, Level = LogLevel.Warning,
			Message = "The repository already exists and the orchestrator is not deleting it")]
		public static partial void WarnSkippingExisting(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.Creating, Level = LogLevel.Trace,
			Message = "Creating the repository")]
		public static partial void TraceCreatingRepository(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.Created, Level = LogLevel.Trace,
			Message = "Repository created")]
		public static partial void TraceRepositoryCreated(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.NotExistsSkipping, Level = LogLevel.Trace,
			Message = "The repository does not exist: it will not be deleted")]
		public static partial void TraceNotExistsSkipping(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.Dropping, Level = LogLevel.Trace,
			Message = "Dropping the repository")]
		public static partial void TraceDroppingRepository(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.Dropped, Level = LogLevel.Trace,
			Message = "The repository was dropped")]
		public static partial void TraceRepositoryDropped(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.SkippingSeed, Level = LogLevel.Warning,
			Message = "The repository already exists: skipping seed operation")]
		public static partial void WarnSkippingSeed(this ILogger logger);

		[LoggerMessage(EventId = LifecycleLogEventIds.Seeding, Level = LogLevel.Trace,
			Message = "Seeding the repository for entity '{EntityType}'")]
		public static partial void TraceSeedingRepository(this ILogger logger, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.SeedCompleted, Level = LogLevel.Trace,
			Message = "Repository seeded for entity '{EntityType}'")]
		public static partial void TraceRepositorySeeded(this ILogger logger, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.NoSeedData, Level = LogLevel.Trace,
			Message = "No seed data available for entity '{EntityType}'")]
		public static partial void TraceNoSeedData(this ILogger logger, string entityType);

		[LoggerMessage(EventId = LifecycleLogEventIds.NotSupportedError, Level = LogLevel.Error,
			Message = "Not Supported Error while {Operation} the repository")]
		public static partial void LogNotSupportedError(this ILogger logger, Exception exception, string operation);

		[LoggerMessage(EventId = LifecycleLogEventIds.RepositoryError, Level = LogLevel.Error,
			Message = "Repository Error while {Operation} the repository")]
		public static partial void LogRepositoryError(this ILogger logger, Exception exception, string operation);

		[LoggerMessage(EventId = LifecycleLogEventIds.GeneralError, Level = LogLevel.Error,
			Message = "Error while {Operation} the repository")]
		public static partial void LogGeneralError(this ILogger logger, Exception exception, string operation);
	}
}
