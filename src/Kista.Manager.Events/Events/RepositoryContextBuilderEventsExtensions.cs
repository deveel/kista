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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista.Events {
	/// <summary>
	/// Shared registration helpers for entity event publishing, used by
	/// both the <see cref="EntityManagerBuilder"/> and
	/// <see cref="RepositoryContextBuilder"/> extension methods to avoid
	/// duplicating the per-entity registration logic.
	/// </summary>
	internal static class EntityEventRegistrar {
		public static void Register(IServiceCollection services, Type entityType, Type keyType, ServiceLifetime lifetime) {
			var publisherInterface = typeof(IEntityEventPublisher<>).MakeGenericType(entityType);
			var inMemoryPublisher = typeof(InMemoryEntityEventPublisher<>).MakeGenericType(entityType);

			services.TryAdd(new ServiceDescriptor(publisherInterface, inMemoryPublisher, lifetime));
			services.TryAdd(new ServiceDescriptor(inMemoryPublisher, inMemoryPublisher, lifetime));

			var interceptorType = typeof(EntityEventInterceptor<,>).MakeGenericType(entityType, keyType);
			var interceptorInterface = typeof(IEntityManagerInterceptor<,>).MakeGenericType(entityType, keyType);
			services.TryAdd(new ServiceDescriptor(interceptorInterface, interceptorType, lifetime));
			services.TryAdd(new ServiceDescriptor(interceptorType, interceptorType, lifetime));

			var singleKeyInterceptorType = typeof(EntityEventInterceptor<>).MakeGenericType(entityType);
			var singleKeyInterceptorInterface = typeof(IEntityManagerInterceptor<>).MakeGenericType(entityType);
			services.TryAdd(new ServiceDescriptor(singleKeyInterceptorInterface, singleKeyInterceptorType, lifetime));
			services.TryAdd(new ServiceDescriptor(singleKeyInterceptorType, singleKeyInterceptorType, lifetime));
		}
	}

	/// <summary>
	/// Extension methods for configuring entity event publishing on an
	/// <see cref="EntityManagerBuilder"/>.
	/// </summary>
	public static class EntityManagerBuilderEventsExtensions {
		/// <summary>
		/// Enables domain event emission for the entity type being
		/// configured by the <see cref="EntityManagerBuilder"/>, registering
		/// the default <see cref="InMemoryEntityEventPublisher{TEntity}"/>
		/// as <see cref="IEntityEventPublisher{TEntity}"/> and the
		/// <see cref="EntityEventInterceptor{TEntity, TKey}"/> in the
		/// operation pipeline.
		/// </summary>
		/// <param name="builder">The entity manager builder.</param>
		/// <param name="lifetime">
		/// The service lifetime for the publisher and interceptor
		/// registrations (default: <see cref="ServiceLifetime.Scoped"/>).
		/// </param>
		/// <returns>The builder for chaining.</returns>
		/// <remarks>
		/// The base <c>Kista.Manager.Events</c> package ships an in-memory
		/// publisher so the event model is usable and testable on its own.
		/// Teams needing real message-bus delivery replace the
		/// <see cref="IEntityEventPublisher{TEntity}"/> registration with
		/// the Hermodr CloudEvents adapter
		/// (<c>Kista.Manager.Hermodr</c>) or a custom implementation.
		/// </remarks>
		public static EntityManagerBuilder WithEntityEvents(
			this EntityManagerBuilder builder,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			ArgumentNullException.ThrowIfNull(builder);

			EntityEventRegistrar.Register(builder.Services, builder.EntityType, builder.EntityKeyType, lifetime);
			return builder;
		}
	}

	/// <summary>
	/// Extension methods for configuring entity event publishing on a
	/// <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderEventsExtensions {
		/// <summary>
		/// Enables domain event emission for all tracked entity types,
		/// registering the default <see cref="InMemoryEntityEventPublisher{TEntity}"/>
		/// and <see cref="EntityEventInterceptor{TEntity, TKey}"/> for each
		/// entity type that has a repository registered.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="lifetime">
		/// The service lifetime for the publisher and interceptor
		/// registrations (default: <see cref="ServiceLifetime.Scoped"/>).
		/// </param>
		/// <returns>The builder for chaining.</returns>
		public static RepositoryContextBuilder WithEntityEvents(
			this RepositoryContextBuilder builder,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			ArgumentNullException.ThrowIfNull(builder);

			foreach (var entityType in builder.RegisteredEntityTypes) {
				var keyType = builder.GetEntityKeyType(entityType);
				EntityEventRegistrar.Register(builder.Services, entityType, keyType, lifetime);
			}

			return builder;
		}
	}
}