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

using Microsoft.Extensions.Logging;

namespace Kista { 
    /// <summary>
	/// Extension methods for logging Entity Framework repository events.
	/// </summary>
	[ExcludeFromCodeCoverage]
    static partial class LoggerExtensions {
        /// <summary>
        /// Logs an unknown error during an entity operation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="error">The exception.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(LogEventIds.UnknownError, LogLevel.Error,
			"An unknwon error has occurred while operating on the entity '{EntityType}'")]
        public static partial void LogUnknownError(this ILogger logger, Exception error, Type entityType);

        /// <summary>
        /// Logs that an entity is being created.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(LogEventIds.CreatingEntity, LogLevel.Trace, 
			"Creating a new entity of type '{EntityType}'")]
        public static partial void TraceCreatingEntity(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs that an entity is being updated.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(LogEventIds.UpdatingEntity, LogLevel.Trace, 
			"Updating an entity of type '{EntityType}' (ID={EntityId})")]
        public static partial void TraceUpdatingEntity(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity is being deleted.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(LogEventIds.DeletingEntity, LogLevel.Trace, 
			"Deleting an entity of type '{EntityType}' (ID={EntityId})")]
        public static partial void TraceDeletingEntity(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity is being found by ID.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(LogEventIds.FindingById, LogLevel.Trace, 
			"Finding an entity of type '{EntityType}' with ID '{EntityId}'")]
        public static partial void TraceFindingById(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity was successfully created.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityCreated, Level = LogLevel.Information, 
                                  Message = "Entity of type '{EntityType}' with ID '{EntityId}' was created")]
        public static partial void LogEntityCreated(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity was successfully updated.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityUpdated, Level = LogLevel.Information, 
                                         Message = "Entity of type '{EntityType}' (ID={EntityId}) updated")]
        public static partial void LogEntityUpdated(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity was successfully deleted.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityDeleted, Level = LogLevel.Information, 
                                                        Message = "Entity of type '{EntityType}' (ID={EntityId}) deleted")]
        public static partial void LogEntityDeleted(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity was found by ID.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityFoundById, Level = LogLevel.Trace, 
                                  Message = "Entity of type '{EntityType}' found with ID '{EntityId}'")]
        public static partial void TraceEntityFoundById(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that an entity was not found by ID.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityNotFoundById, Level = LogLevel.Trace, 
                                                        Message = "Entity of type '{EntityType}' not found with ID '{EntityId}'")]
        public static partial void TraceEntityNotFoundById(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs that the user context is not set.
        /// </summary>
        /// <param name="logger">The logger.</param>
        [LoggerMessage(EventId = LogEventIds.UserNotSet, Level = LogLevel.Warning, 
            Message = "User context not set for the current operation")]
        public static partial void WarnUserNotSet(this ILogger logger);
        
        /// <summary>
        /// Logs a warning that an entity was not found.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityNotFound, Level = LogLevel.Warning, 
            Message = "Entity of type '{EntityType}' with ID '{EntityId}' not found")]
        public static partial void WarnEntityNotFound(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs a warning that an entity was not deleted.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityNotDeleted, Level = LogLevel.Warning, 
                       Message = "Entity of type '{EntityType}' with ID '{EntityId}' not deleted")]
        public static partial void WarnEntityNotDeleted(this ILogger logger, Type entityType, object entityId);

        /// <summary>
        /// Logs a warning that an entity was not updated.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityNotUpdated, Level = LogLevel.Warning, 
                                         Message = "Entity of type '{EntityType}' with ID '{EntityId}' not updated")]
        public static partial void WarnEntityNotUpdated(this ILogger logger, Type entityType, object entityId);
        
        /// <summary>
        /// Logs a warning that the entity owner does not match the current user.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="userId">The current user ID.</param>
        /// <param name="ownerId">The entity owner ID.</param>
        [LoggerMessage(EventId = LogEventIds.EntityOwnerNotMatching, Level = LogLevel.Warning,
            Message = "Entity of type '{EntityType}' with ID '{EntityId}' is owned by user '{OwnerId}' not matching repository user is '{UserId}'")]
        public static partial void WarnOwnerNotMatchingUser(this ILogger logger, Type entityType, object entityId, object userId, object ownerId);
    }
}
