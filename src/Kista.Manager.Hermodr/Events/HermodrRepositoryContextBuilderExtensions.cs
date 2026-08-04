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

using Hermodr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Kista.Events {
	/// <summary>
	/// Shared registration helpers for the Hermodr-based entity event
	/// publisher, used by both the <see cref="EntityManagerBuilder"/>
	/// and <see cref="RepositoryContextBuilder"/> extension methods.
	/// </summary>
	internal static class HermodrEventRegistrar {
		public static void Register(
			IServiceCollection services,
			Type entityType,
			Type keyType,
			ServiceLifetime lifetime) {
			var publisherInterface = typeof(IEntityEventPublisher<>).MakeGenericType(entityType);
			var hermodrPublisher = typeof(HermodrEventPublisher<>).MakeGenericType(entityType);

			services.TryAdd(new ServiceDescriptor(publisherInterface, hermodrPublisher, lifetime));
			services.TryAdd(new ServiceDescriptor(hermodrPublisher, hermodrPublisher, lifetime));

			var interceptorType = typeof(EntityEventInterceptor<,>).MakeGenericType(entityType, keyType);
			var interceptorInterface = typeof(IEntityManagerInterceptor<,>).MakeGenericType(entityType, keyType);
			services.TryAdd(new ServiceDescriptor(interceptorInterface, interceptorType, lifetime));
			services.TryAdd(new ServiceDescriptor(interceptorType, interceptorType, lifetime));

			var singleKeyInterceptorType = typeof(EntityEventInterceptor<>).MakeGenericType(entityType);
			var singleKeyInterceptorInterface = typeof(IEntityManagerInterceptor<>).MakeGenericType(entityType);
			services.TryAdd(new ServiceDescriptor(singleKeyInterceptorInterface, singleKeyInterceptorType, lifetime));
			services.TryAdd(new ServiceDescriptor(singleKeyInterceptorType, singleKeyInterceptorType, lifetime));

			services.TryAdd(new ServiceDescriptor(
				typeof(IConfigureOptions<HermodrEventsOptions>),
				sp => new ConfigureHermodrEventsOptions(),
				ServiceLifetime.Singleton));
		}

		private sealed class ConfigureHermodrEventsOptions : IConfigureOptions<HermodrEventsOptions> {
			public void Configure(HermodrEventsOptions options) {
			}
		}
	}

	/// <summary>
	/// Extension methods for configuring Hermodr-based entity event
	/// publishing on an <see cref="EntityManagerBuilder"/>.
	/// </summary>
	public static class HermodrEntityManagerBuilderExtensions {
		/// <summary>
		/// Enables domain event emission for the entity type being
		/// configured by the <see cref="EntityManagerBuilder"/>, bridging
		/// the event model with the Hermodr CloudEvents framework.
		/// </summary>
		/// <param name="builder">The entity manager builder.</param>
		/// <param name="configure">
		/// An optional delegate to configure the
		/// <see cref="HermodrEventsOptions"/> controlling the CloudEvent
		/// mapping.
		/// </param>
		/// <param name="lifetime">
		/// The service lifetime for the publisher and interceptor
		/// registrations (default: <see cref="ServiceLifetime.Scoped"/>).
		/// </param>
		/// <returns>The builder for chaining.</returns>
		/// <remarks>
		/// <para>
		/// This call registers the Hermodr <see cref="IEventPublisher"/>
		/// pipeline (via <c>AddEventPublisher()</c> from
		/// <c>Hermodr.Publisher</c>), the
		/// <see cref="HermodrEventPublisher{TEntity}"/> as
		/// <see cref="IEntityEventPublisher{TEntity}"/>, and the
		/// <see cref="EntityEventInterceptor{TEntity, TKey}"/> in the
		/// operation pipeline.
		/// </para>
		/// <para>
		/// When a transport channel (Azure Service Bus, RabbitMQ,
		/// MassTransit, Webhook) has been registered separately through
		/// the Hermodr publisher builder, the CloudEvents are dispatched
		/// through it; otherwise the default in-process channel is used.
		/// </para>
		/// </remarks>
		public static EntityManagerBuilder WithHermodrEvents(
			this EntityManagerBuilder builder,
			Action<HermodrEventsOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			ArgumentNullException.ThrowIfNull(builder);

			var options = new HermodrEventsOptions();
			configure?.Invoke(options);

			builder.Services.AddEventPublisher();
			builder.Services.AddSingleton(options);

			HermodrEventRegistrar.Register(builder.Services, builder.EntityType, builder.EntityKeyType, lifetime);
			return builder;
		}
	}

	/// <summary>
	/// Extension methods for configuring Hermodr-based entity event
	/// publishing on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class HermodrRepositoryContextBuilderExtensions {
		/// <summary>
		/// Enables Hermodr-based domain event emission for all tracked
		/// entity types, registering the
		/// <see cref="HermodrEventPublisher{TEntity}"/> and
		/// <see cref="EntityEventInterceptor{TEntity, TKey}"/> for each
		/// entity type that has a repository registered.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="configure">
		/// An optional delegate to configure the
		/// <see cref="HermodrEventsOptions"/> controlling the CloudEvent
		/// mapping.
		/// </param>
		/// <param name="lifetime">
		/// The service lifetime for the publisher and interceptor
		/// registrations (default: <see cref="ServiceLifetime.Scoped"/>).
		/// </param>
		/// <returns>The builder for chaining.</returns>
		public static RepositoryContextBuilder WithHermodrEvents(
			this RepositoryContextBuilder builder,
			Action<HermodrEventsOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			ArgumentNullException.ThrowIfNull(builder);

			var options = new HermodrEventsOptions();
			configure?.Invoke(options);

			builder.Services.AddEventPublisher();
			builder.Services.AddSingleton(options);

			foreach (var entityType in builder.RegisteredEntityTypes) {
				var keyType = builder.GetEntityKeyType(entityType);
				HermodrEventRegistrar.Register(builder.Services, entityType, keyType, lifetime);
			}

			return builder;
		}
	}
}