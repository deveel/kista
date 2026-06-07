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
using Deveel;

namespace Kista {
	/// <summary>
	/// Extends the <see cref="EntityManager{TEntity}"/> to provide
	/// filtering capabilities using a dynamic LINQ expression.
	/// </summary>
	/// <seealso cref="DynamicLinqFilter"/>
	public static class EntityManagerExtensions {
		/// <summary>
		/// Finds the first entity in the repository that matches the given
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to find.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to use to find the entity.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="OperationResult{TEntity}"/>
		/// containing the first entity that matches the given expression,
		/// or a failure result if no entity was found or an error occurred.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with EntityManager's query methods instead. Query capabilities should not be exposed through the EntityManager contract.", false)]
		public static ValueTask<OperationResult<TEntity>> FindFirstAsync<TEntity>(this EntityManager<TEntity> manager, string expression, CancellationToken? cancellationToken = null)
			where TEntity : class
			=> manager.FindFirstAsync(new Query(new DynamicLinqFilter(expression)), cancellationToken);

		/// <summary>
		/// Finds the first entity in the repository that matches the given
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to find.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to use to find the entity.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="OperationResult{TEntity}"/>
		/// containing the first entity that matches the given expression,
		/// or a failure result if no entity was found or an error occurred.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with EntityManager's query methods instead. Query capabilities should not be exposed through the EntityManager contract.", false)]
		public static ValueTask<OperationResult<TEntity>> FindFirstAsync<TEntity, TKey>(this EntityManager<TEntity, TKey> manager, string expression, CancellationToken? cancellationToken = null)
			where TEntity : class
			where TKey : notnull
			=> manager.FindFirstAsync(new Query(new DynamicLinqFilter(expression)), cancellationToken);


		/// <summary>
		/// Finds a range of entities in the repository that matches the given
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to find.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to use to find the entity.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of entities that matches the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with EntityManager's query methods instead. Query capabilities should not be exposed through the EntityManager contract.", false)]
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity>(this EntityManager<TEntity> manager, string expression, CancellationToken? cancellationToken = null)
			where TEntity : class
			=> manager.FindAllAsync(new Query(new DynamicLinqFilter(expression)), cancellationToken);

		/// <summary>
		/// Finds a range of entities in the repository that matches the given
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to find.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to use to find the entity.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of entities that matches the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with EntityManager's query methods instead. Query capabilities should not be exposed through the EntityManager contract.", false)]
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity, TKey>(this EntityManager<TEntity, TKey> manager, string expression, CancellationToken? cancellationToken = null)
			where TEntity : class
			where TKey : notnull
			=> manager.FindAllAsync(new Query(new DynamicLinqFilter(expression)), cancellationToken);


		/// <summary>
		/// Counts the number of entities in the repository that matches the
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to count.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to use to count the entities.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that matches the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with EntityManager's query methods instead. Query capabilities should not be exposed through the EntityManager contract.", false)]
		public static ValueTask<long> CountAsync<TEntity>(this EntityManager<TEntity> manager, string expression, CancellationToken? cancellationToken = null)
			where TEntity : class
			=> manager.CountAsync(new DynamicLinqFilter(expression), cancellationToken);

		/// <summary>
		/// Counts the number of entities in the repository that matches the
		/// dynamic LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to count.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to use to count the entities.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that matches the given expression.
		/// </returns>
		[ExcludeFromCodeCoverage]
		[Obsolete("Use DynamicLinqFilter directly with EntityManager's query methods instead. Query capabilities should not be exposed through the EntityManager contract.", false)]
		public static ValueTask<long> CountAsync<TEntity, TKey>(this EntityManager<TEntity, TKey> manager, string expression, CancellationToken? cancellationToken = null)
			where TEntity : class
			where TKey : notnull
			=> manager.CountAsync(new DynamicLinqFilter(expression), cancellationToken);

	}
}