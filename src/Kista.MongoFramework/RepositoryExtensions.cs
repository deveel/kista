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

using System.Linq.Expressions;

using MongoDB.Driver.GeoJsonObjectModel;

namespace Kista {
	/// <summary>
	/// Extends the <see cref="IRepository{TEntity}"/> interface
	/// to provide additional methods for MongoDB.
	/// </summary>
	public static class RepositoryExtensions {
		/// <summary>
		/// Finds the first entity in the repository that matches the given
		/// geo-distance filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to be found.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to search into.
		/// </param>
		/// <param name="field">
		/// The expression that identifies the field to be used for the
		/// identification of the location of the entity.
		/// </param>
		/// <param name="point">
		/// The point to be used as the center of the search.
		/// </param>
		/// <param name="maxDistance">
		/// The maximum distance from the center point to search for.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// matches the given filter, or <c>null</c> if no entity is found.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given repository is not a <see cref="MongoRepository{TEntity}"/>.
		/// </exception>
		/// <seealso cref="MongoGeoDistanceFilter{TEntity}"/>
		public static ValueTask<TEntity?> FindFirstByGeoDistanceAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, object>> field, GeoPoint point, double? maxDistance = null)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository, nameof(repository));

			if (!(repository is MongoRepository<TEntity> mongoRepository))
				throw new ArgumentException($"The repository is not a {nameof(MongoRepository<TEntity>)}");

			return mongoRepository.FindFirstAsync(new Query(new MongoGeoDistanceFilter<TEntity>(field, point, maxDistance)));
		}

		/// <summary>
		/// Finds the first entity in the repository that matches the given
		/// geo-distance filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to be found.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to search into.
		/// </param>
		/// <param name="field">
		/// The expression that identifies the field to be used for the
		/// identification of the location of the entity.
		/// </param>
		/// <param name="point">
		/// The point to be used as the center of the search.
		/// </param>
		/// <param name="maxDistance">
		/// The maximum distance from the center point to search for.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// matches the given filter, or <c>null</c> if no entity is found.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given repository is not a <see cref="MongoRepository{TEntity}"/>.
		/// </exception>
		/// <seealso cref="MongoGeoDistanceFilter{TEntity}"/>
		public static ValueTask<TEntity?> FindFirstByGeoDistanceAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, object>> field, GeoPoint point, double? maxDistance = null)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository, nameof(repository));

			if (!(repository is MongoRepository<TEntity, TKey> mongoRepository))
				throw new ArgumentException($"The repository is not a {nameof(MongoRepository<TEntity>)}");

			return mongoRepository.FindFirstAsync(new Query(new MongoGeoDistanceFilter<TEntity>(field, point, maxDistance)));
		}


		/// <summary>
		/// Finds all the entities in the repository that match the given
		/// geo-distance filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to be found.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to search into.
		/// </param>
		/// <param name="field">
		/// The expression that identifies the field to be used for the
		/// identification of the location of the entity.
		/// </param>
		/// <param name="point">
		/// The point to be used as the center of the search.
		/// </param>
		/// <param name="maxDistance">
		/// The maximum distance from the center point to search for.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IList{TEntity}"/> that
		/// is the result of the search.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given repository is not a <see cref="MongoRepository{TEntity}"/>.
		/// </exception>
		public static ValueTask<IList<TEntity>> FindAllByGeoDistanceAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, object>> field, GeoPoint point, double? maxDistance = null)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository, nameof(repository));

			if (!(repository is MongoRepository<TEntity> mongoRepository))
				throw new ArgumentException($"The repository is not a {nameof(MongoRepository<TEntity>)}");

			return mongoRepository.FindAllAsync(new MongoGeoDistanceFilter<TEntity>(field, point, maxDistance));
		}

		/// <summary>
		/// Finds all the entities in the repository that match the given
		/// geo-distance filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to be found.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to search into.
		/// </param>
		/// <param name="field">
		/// The expression that identifies the field to be used for the
		/// identification of the location of the entity.
		/// </param>
		/// <param name="point">
		/// The point to be used as the center of the search.
		/// </param>
		/// <param name="maxDistance">
		/// The maximum distance from the center point to search for.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IList{TEntity}"/> that
		/// is the result of the search.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given repository is not a <see cref="MongoRepository{TEntity, TKey}"/>.
		/// </exception>
		public static ValueTask<IList<TEntity>> FindAllByGeoDistanceAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, object>> field, GeoPoint point, double? maxDistance = null)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository, nameof(repository));

			if (!(repository is MongoRepository<TEntity, TKey> mongoRepository))
				throw new ArgumentException($"The repository is not a {nameof(MongoRepository<TEntity>)}");

			return mongoRepository.FindAllAsync(new MongoGeoDistanceFilter<TEntity>(field, point, maxDistance));
		}

	}
}
