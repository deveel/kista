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

namespace Kista.Events {
	/// <summary>
	/// Event identifiers for domain event emission log messages from the
	/// <c>Kista.Manager.Events</c> package.
	/// </summary>
	[ExcludeFromCodeCoverage]
	internal static class EntityEventLogEventIds {
		public const int EntityEventPublished = 1008;
		public const int EntityEventPublishFailed = -10025;
	}

	/// <summary>
	/// Logger extension methods for domain event emission.
	/// </summary>
	[ExcludeFromCodeCoverage]
	internal static class EntityEventLoggerExtensions {
		public static void LogEntityEventPublished(this ILogger logger, Type entityType, object? entityKey, Type eventType) {
			logger.LogDebug(EntityEventLogEventIds.EntityEventPublished,
				"A domain event of type {EventType} was published for the entity of type {EntityType} identified by {EntityKey}.",
				eventType, entityType, entityKey);
		}

		public static void LogEntityEventPublishFailed(this ILogger logger, Exception error, Type entityType, object? entityKey, Type eventType) {
			logger.LogError(EntityEventLogEventIds.EntityEventPublishFailed, error,
				"Failed to publish a domain event of type {EventType} for the entity of type {EntityType} identified by {EntityKey}.",
				eventType, entityType, entityKey);
		}
	}
}