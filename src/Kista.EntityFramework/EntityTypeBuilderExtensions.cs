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

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.Linq.Expressions;

namespace Kista
{
	/// <summary>
	/// Extensions for the <see cref="EntityTypeBuilder{TEntity}"/> class
	/// to provide additional configuration for entities that have an owner.
	/// </summary>
	public static class EntityTypeBuilderExtensions
	{
		/// <summary>
		/// Configures the entity to have a query filter that restricts the
		/// data to the owner of the entity.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity that has an owner.
		/// </typeparam>
		/// <typeparam name="TUserKey">
		/// The type of the key that identifies the user.
		/// </typeparam>
		/// <param name="builder">
		/// A builder to configure the entity.
		/// </param>
		/// <param name="userAccessor">
		/// A service that provides information about the current user
		/// that is using the application.
		/// </param>
		/// <returns>
		/// Returns the builder to continue the configuration.
		/// </returns>
		/// <seealso cref="HasOwnerFilter{TEntity, TUserKey}(EntityTypeBuilder{TEntity}, string, IUserAccessor{TUserKey})"/>
		public static EntityTypeBuilder<TEntity> HasOwnerFilter<TEntity, TUserKey>(this EntityTypeBuilder<TEntity> builder, IUserAccessor<TUserKey> userAccessor)
			where TEntity : class, IHaveOwner<TUserKey>
			=> builder.HasOwnerFilter("", userAccessor);

		/// <summary>
		/// Configures the entity to have a query filter that restricts the
		/// data to the owner of the entity.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity that has an owner.
		/// </typeparam>
		/// <typeparam name="TUserKey">
		/// The type of the key that identifies the user.
		/// </typeparam>
		/// <param name="builder">
		/// A builder to configure the entity.
		/// </param>
		/// <param name="propertyName">
		/// The name of the property that holds the owner identifier.
		/// </param>
		/// <param name="userAccessor">
		/// A service that provides information about the current user
		/// that is using the application.
		/// </param>
		/// <returns>
		/// Returns the builder to continue the configuration.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Throws when the property name is not found in the entity type.
		/// </exception>
		public static EntityTypeBuilder<TEntity> HasOwnerFilter<TEntity, TUserKey>(this EntityTypeBuilder<TEntity> builder, string propertyName, IUserAccessor<TUserKey> userAccessor)
			where TEntity : class, IHaveOwner<TUserKey>
		{
			if (string.IsNullOrWhiteSpace(propertyName))
			{
				foreach(var property in builder.Metadata.GetDeclaredProperties())
				{
					if (property.PropertyInfo == null)
						continue;

					if (Attribute.IsDefined(property.PropertyInfo, typeof(DataOwnerAttribute)))
					{
						propertyName = property.Name;
						break;
					}
				}
			} else {
				var fieldMetadata = builder.Metadata.FindDeclaredProperty(propertyName);
				if (fieldMetadata == null)
					throw new RepositoryException($"The property '{propertyName}' was not found in the entity type '{typeof(TEntity).Name}'");
			}

			if (string.IsNullOrWhiteSpace(propertyName))
				throw new RepositoryException($"The property name was not specified and no property was found in the entity type '{typeof(TEntity).Name}'");

			var varRef = Expression.Variable(typeof(TEntity), "x");
			var propertyRef = Expression.Property(varRef, propertyName);
			var getUserId = Expression.Call(
				Expression.Constant(userAccessor),
				typeof(IUserAccessor<TUserKey>)
				.GetMethod(nameof(IUserAccessor<TUserKey>.GetUserId)));

			LambdaExpression lambda;

			if (typeof(TUserKey) == typeof(string))
			{
				lambda = Expression.Lambda(Expression.Equal(propertyRef, getUserId), varRef);
				
			} else
			{
				var equalsMethod = typeof(TUserKey).GetMethod(nameof(object.Equals), new[] { typeof(object) });
				lambda = Expression.Lambda(Expression.Call(propertyRef, equalsMethod, getUserId), varRef);
			}

			return builder.HasQueryFilter(lambda);
		}
	}
}
