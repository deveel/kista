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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// Provides extension methods to configure the soft-delete query filter
	/// on Entity Framework Core entity types that implement
	/// <see cref="ISoftDeletable"/>.
	/// </summary>
	public static class SoftDeleteModelBuilderExtensions {
		/// <summary>
		/// Configures the entity to have a global query filter that
		/// transparently excludes soft-deleted records from all regular
		/// queries, when the entity implements <see cref="ISoftDeletable"/>.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to configure.
		/// </typeparam>
		/// <param name="builder">
		/// The <see cref="EntityTypeBuilder{TEntity}"/> used to configure
		/// the entity.
		/// </param>
		/// <returns>
		/// Returns the builder to continue the configuration. If the entity
		/// does not implement <see cref="ISoftDeletable"/>, the builder is
		/// returned unchanged.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Call this method from your <c>OnModelCreating</c> override or
		/// from an <see cref="IEntityTypeConfiguration{TEntity}"/>
		/// implementation to enable transparent soft-delete filtering at
		/// the Entity Framework level.
		/// </para>
		/// <para>
		/// The filter applied is <c>e =&gt; !e.IsDeleted</c>. Queries that
		/// need to include soft-deleted records must use the
		/// <see cref="QueryBuilder{TEntity}.IncludeDeleted"/> / 
		/// <see cref="QueryBuilder{TEntity}.OnlyDeleted"/> modifiers,
		/// which are handled by the repository driver through
		/// <c>IgnoreQueryFilters()</c>.
		/// </para>
		/// </remarks>
		public static EntityTypeBuilder<TEntity> HasSoftDeleteFilter<TEntity>(this EntityTypeBuilder<TEntity> builder)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(builder);

			if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
				return builder;

			var parameter = Expression.Parameter(typeof(TEntity), "e");
			var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
			var notDeleted = Expression.Not(property);
			var lambda = Expression.Lambda(notDeleted, parameter);

			return builder.HasQueryFilter(lambda);
		}

		/// <summary>
		/// Configures all entity types in the model that implement
		/// <see cref="ISoftDeletable"/> to have the soft-delete global
		/// query filter applied.
		/// </summary>
		/// <param name="modelBuilder">
		/// The <see cref="ModelBuilder"/> used to configure the model.
		/// </param>
		/// <returns>
		/// Returns the builder to continue the configuration.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Call this method once from your <c>OnModelCreating</c> override
		/// to apply the soft-delete filter to every entity in the model
		/// that implements <see cref="ISoftDeletable"/>.
		/// </para>
		/// </remarks>
		public static ModelBuilder HasSoftDeleteFilter(this ModelBuilder modelBuilder) {
			ArgumentNullException.ThrowIfNull(modelBuilder);

			var softDeletableTypes = modelBuilder.Model.GetEntityTypes()
				.Where(entityType => typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
				.Select(entityType => entityType.ClrType);

			foreach (var clrType in softDeletableTypes) {
				var entityBuilder = modelBuilder.Entity(clrType);

				var parameter = Expression.Parameter(clrType, "e");
				var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
				var notDeleted = Expression.Not(property);
				var lambda = Expression.Lambda(notDeleted, parameter);

				entityBuilder.HasQueryFilter(lambda);
			}

			return modelBuilder;
		}
	}
}