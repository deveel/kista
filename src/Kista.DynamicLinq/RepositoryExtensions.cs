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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Dynamic.Core;

namespace Kista {
	/// <summary>
	/// Provides a set of extension methods for the <see cref="IRepository{TEntity}"/> contract
	/// that allows to perform filtering using a dynamic LINQ expression.
	/// </summary>
	public static class RepositoryExtensions {
		#region Find

		/// <summary>
		/// Finds a single entity in the repository that matches the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matches the given expression,
		/// otherwise <c>null</c> if no entity is found.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<TEntity?> FindFirstAsync<TEntity>(this IRepository<TEntity> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindFirstAsync(new DynamicLinqFilter(paramName, expression), cancellationToken);

		/// <summary>
		/// Finds a single entity in the repository that matches the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matches the given expression,
		/// otherwise <c>null</c> if no entity is found.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindFirstAsync(new DynamicLinqFilter(paramName, expression), cancellationToken);


		/// <summary>
		/// Finds a single entity in the repository that matches the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matches the given expression,
		/// otherwise <c>null</c> if no entity is found.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<TEntity?> FindFirstAsync<TEntity>(this IRepository<TEntity> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindFirstAsync(new DynamicLinqFilter(expression), cancellationToken);

		/// <summary>
		/// Finds a single entity in the repository that matches the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matches the given expression,
		/// otherwise <c>null</c> if no entity is found.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindFirstAsync(new DynamicLinqFilter(expression), cancellationToken);


		#endregion

		#region FindAll

		/// <summary>
		/// Finds all the entities in the repository that match the given 
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> that match the 
		/// given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<IReadOnlyList<TEntity>> FindAllAsync<TEntity>(this IRepository<TEntity> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindAllAsync(new DynamicLinqFilter(paramName, expression), cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository that match the given 
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> that match the 
		/// given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<IReadOnlyList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindAllAsync(new DynamicLinqFilter(paramName, expression), cancellationToken);


		/// <summary>
		/// Finds all the entities in the repository that match the given
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> that match the
		/// given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<IReadOnlyList<TEntity>> FindAllAsync<TEntity>(this IRepository<TEntity> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindAllAsync(new DynamicLinqFilter(expression), cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository that match the given
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to find in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to perform the search.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> that match the
		/// given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<IReadOnlyList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindAllAsync(new DynamicLinqFilter(expression), cancellationToken);


		#endregion

		#region Count

		/// <summary>
		/// Counts the number of entities in the repository that match 
		/// the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to count in the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository that contains the entities 
		/// to count.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that match the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<long> CountAsync<TEntity>(this IRepository<TEntity> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.AsFilterable().CountAsync(new DynamicLinqFilter(paramName, expression), cancellationToken);

		/// <summary>
		/// Counts the number of entities in the repository that match 
		/// the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to count in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository that contains the entities 
		/// to count.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that match the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<long> CountAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.AsFilterable().CountAsync(new DynamicLinqFilter(paramName, expression), cancellationToken);


		/// <summary>
		/// Counts the number of entities in the repository that match
		/// the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to count in the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository that contains the entities
		/// to be counted.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that match the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<long> CountAsync<TEntity>(this IRepository<TEntity> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.AsFilterable().CountAsync(new DynamicLinqFilter(expression), cancellationToken);

		/// <summary>
		/// Counts the number of entities in the repository that match
		/// the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to count in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository that contains the entities
		/// to be counted.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that match the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<long> CountAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.AsFilterable().CountAsync(new DynamicLinqFilter(expression), cancellationToken);


		#endregion

		#region Exists

		/// <summary>
		/// Checks if the repository contains at least one entity that matches
		/// the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to check in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to check.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the repository contains at least one entity
		/// that matches the given expression, otherwise <c>false</c>.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<bool> ExistsAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string paramName, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.ExistsAsync<TEntity, TKey>(new DynamicLinqFilter(paramName, expression), cancellationToken);

		/// <summary>
		/// Checks if the repository contains at least one entity that matches
		/// the given dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to check in the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to check.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <remarks>
		/// The default parameter name used in the expression is 
		/// <see cref="DynamicLinqFilter.DefaultParameterName"/>.
		/// </remarks>
		/// <returns>
		/// Returns <c>true</c> if the repository contains at least one entity
		/// that matches the given expression, otherwise <c>false</c>.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with the repository's query methods instead. Query capabilities should not be exposed through the IRepository contract.", false)]
		public static ValueTask<bool> ExistsAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string expression, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.ExistsAsync<TEntity, TKey>(DynamicLinqFilter.DefaultParameterName, expression, cancellationToken);

		#endregion
	}
}
