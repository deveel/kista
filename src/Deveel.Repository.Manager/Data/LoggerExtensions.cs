// Copyright 2023-2025 Antonello Provenzano
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

using Microsoft.Extensions.Logging;

namespace Deveel.Data {
/// <summary>
	/// Extension methods for logging entity manager events.
	/// </summary>
	static partial class LoggerExtensions {
        /// <summary>
        /// Logs an unknown error during a management operation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="exception">The exception.</param>
        [LoggerMessage(EntityManagerEventIds.UnknownError, LogLevel.Error, "An unknown error occurred during a management operation of entities of type {EntityType}.")]
		public static partial void LogUnknownError(this ILogger logger, Type entityType, Exception exception);

        /// <summary>
        /// Logs an unknown error during an operation on a specific entity.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="exception">The exception.</param>
        [LoggerMessage(EntityManagerEventIds.EntityUnknownError, LogLevel.Error, "An unknown error occurred during the operation on the entity of type {EntityType} identified by {EntityId}")]
		public static partial void LogEntityUnknownError(this ILogger logger, Type entityType, object? entityId, Exception exception);

        /// <summary>
        /// Logs that an entity failed validation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(EntityManagerEventIds.EntityNotValid, LogLevel.Error, "The entity of type {EntityType} to be added or updated is not valid.")]
		public static partial void LogEntityNotValid(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs an error when an entity was not cached.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="error">The exception.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityNotCached, LogLevel.Error, "The entity of type {EntityType} identified by {EntityId} was not cached.")]
		public static partial void LogEntityNotCached(this ILogger logger, Exception error, Type entityType, object? entityId);

        /// <summary>
        /// Logs an error when an entity was not evicted from cache.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="error">The exception.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityNotEvicted, LogLevel.Error, "The entity of type {EntityType} identified by {EntityId} was not removed from the cache.")]
		public static partial void LogEntityNotEvicted(this ILogger logger, Exception error, Type entityType, object? entityId);

        /// <summary>
        /// Logs that an entity was not modified during an operation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityNotModified, LogLevel.Warning, "The entity of type {EntityType} identified by {EntityId} was not modified during the operation.")]
		public static partial void LogEntityNotModified(this ILogger logger, Type entityType, object? entityId);

        /// <summary>
        /// Logs that an entity was not found in the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityNotFound, LogLevel.Warning, "The entity of type {EntityType} identified by {EntityId} was not found in the repository.")]
		public static partial void LogEntityNotFound(this ILogger logger, Type entityType, object? entityId);

        /// <summary>
        /// Logs that an entity was not removed from the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityNotRemoved, LogLevel.Warning, "The entity of type {EntityType} identified by {EntityId} was not removed from the repository.")]
		public static partial void LogEntityNotRemoved(this ILogger logger, Type entityType, object? entityId);

        /// <summary>
        /// Logs that an entity is being added to the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(EntityManagerEventIds.AddingEntity, LogLevel.Debug, "An entity of type {EntityType} is being added to the repository.")]
		public static partial void LogAddingEntity(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs that an entity is being updated in the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.UpdatingEntity, LogLevel.Debug, "The entity of type {EntityType} identified by {EntityId} is being updated in the repository.")]
		public static partial void LogUpdatingEntity(this ILogger logger, Type entityType, object? entityId);

        /// <summary>
        /// Logs that an entity is being removed from the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.RemovingEntity, LogLevel.Debug, "The entity of tpye {EntityType} identified by {EntityId} is being removed from the repository.")]
		public static partial void LogRemovingEntity(this ILogger logger, Type entityType, object? entityId);

        /// <summary>
        /// Logs that a range of entities is being added to the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(EntityManagerEventIds.AddingEntityRange, LogLevel.Debug, "A range of entities of type {EntityType} is being added to the repository.")]
		public static partial void LogAddingEntityRange(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs that a range of entities is being removed from the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        [LoggerMessage(EntityManagerEventIds.RemovingEntityRange, LogLevel.Debug, "A range of entities is being removed from the repository.")]
		public static partial void LogRemovingEntityRange(this ILogger logger);

        /// <summary>
        /// Logs that an entity is being found by key.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityKey">The entity key.</param>
        [LoggerMessage(EntityManagerEventIds.FindingEntityByKey, LogLevel.Debug, "Attempting to find an entity of type {EntityType} using the key {EntityKey}")]
		public static partial void LogFindingEntityByKey(this ILogger logger, Type entityType, object? entityKey);

        /// <summary>
        /// Logs that the first entity matching a query is being searched.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(EntityManagerEventIds.FindingFirstEntityByQuery, LogLevel.Debug, "Attempting to find an entity of type {EntityType} using a query filter")]
		public static partial void LogFindingFirstEntityByQuery(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs that all entities matching a query are being searched.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(EntityManagerEventIds.FindingAllEntitiesByQuery, LogLevel.Debug, "Attempting to find a range of entities of type {EntityType} using a query filter")]
		public static partial void LogFindingAllEntitiesByQuery(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs that a page of entities is being requested.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        [LoggerMessage(EntityManagerEventIds.GettingEntityPage, LogLevel.Debug, "Page {PageNumber} of {PageSize} entities of type {EntityType} is being requested from the repository")]
		public static partial void LogGettingEntityPage(this ILogger logger, Type entityType, int pageNumber, int pageSize);

        /// <summary>
        /// Logs that the count of entities is being requested.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        [LoggerMessage(EntityManagerEventIds.CountingEntities, LogLevel.Debug, "The count of entities of type {EntityType} is being requested from the repository")]
		public static partial void LogCountingEntities(this ILogger logger, Type entityType);

        /// <summary>
        /// Logs that an entity was found by key.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityKey">The entity key.</param>
        [LoggerMessage(EntityManagerEventIds.EntityFoundByKey, LogLevel.Debug, "An entity of type {EntityType} identified by {EntityKey} has been found repository")]
		public static partial void LogEntityFoundByKey(this ILogger logger, Type entityType, object? entityKey);

        /// <summary>
        /// Logs that an entity was found by query.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="query">The query.</param>
        [LoggerMessage(EntityManagerEventIds.EntityFoundByQuery, LogLevel.Debug, "A first entity of type {EntityType} matching the query {Query} has been found in the repository")]
		public static partial void LogEntityFoundByQuery(this ILogger logger, Type entityType, object? query);

        /// <summary>
        /// Logs that an entity was added to the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityAdded, LogLevel.Information, "The entity {EntityId} was added to the repository.")]
		public static partial void LogEntityAdded(this ILogger logger, object? entityId);

        /// <summary>
        /// Logs that a range of entities was added to the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        [LoggerMessage(EntityManagerEventIds.EntityRangeAdded, LogLevel.Information, "A range of entities was added to the repository.")]
        public static partial void LogEntityRangeAdded(this ILogger logger);

        /// <summary>
        /// Logs that an entity was removed from the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityRemoved, LogLevel.Information, "The entity {EntityId} was removed from the repository.")]
		public static partial void LogEntityRemoved(this ILogger logger, object? entityId);

        /// <summary>
        /// Logs that a range of entities was removed from the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        [LoggerMessage(EntityManagerEventIds.EntityRangeRemoved, LogLevel.Information, "A range of entities was removed from the repository.")]
        public static partial void LogEntityRangeRemoved(this ILogger logger);

        /// <summary>
        /// Logs that an entity was updated in the repository.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="entityId">The entity ID.</param>
        [LoggerMessage(EntityManagerEventIds.EntityUpdated, LogLevel.Information, "The entity {EntityId} was updated in the repository.")]
		public static partial void LogEntityUpdated(this ILogger logger, object? entityId);
	}
}
