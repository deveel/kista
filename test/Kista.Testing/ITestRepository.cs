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

namespace Kista {

	/// <summary>
	/// A test-support interface that exposes the protected filterable
	/// pipeline of <see cref="Repository{TEntity, TKey}"/> through public
	/// passthroughs, so test classes and shared test suites can exercise
	/// the data-layer query translation without relying on
	/// <c>InternalsVisibleTo</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Test stub subclasses derive from a concrete
	/// <see cref="Repository{TEntity, TKey}"/> implementation (for example
	/// <c>InMemoryRepository</c>, <c>EntityRepository</c>,
	/// <c>MongoRepository</c>) and implement this interface by forwarding
	/// each method to the corresponding <c>protected</c> base-class entry
	/// point. Because subclasses can call <c>protected</c> members, no
	/// <c>InternalsVisibleTo</c> attribute is required.
	/// </para>
	/// <para>
	/// The interface lives in <c>Kista.Testing</c> (a non-test shared
	/// library referenced by every driver test project) so the abstract
	/// <see cref="RepositoryTestSuite{TPerson, TKey, TRelationship}"/>
	/// can cast the resolved <see cref="IRepository{TEntity, TKey}"/> to
	/// <see cref="ITestRepository{TEntity, TKey}"/> and drive the
	/// filterable pipeline from its shared tests.
	/// </para>
	/// </remarks>
	/// <typeparam name="TEntity">The type of entity handled by the repository.</typeparam>
	/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
	public interface ITestRepository<TEntity, TKey> where TEntity : class {
		/// <summary>
		/// Finds the first entity matching the given query, forwarding to
		/// the protected <c>FindFirstAsync(IQuery, CancellationToken)</c>
		/// entry point of the underlying repository.
		/// </summary>
		ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default);

		/// <summary>
		/// Finds all the entities matching the given query, forwarding to
		/// the protected <c>FindAllAsync(IQuery, CancellationToken)</c>
		/// entry point of the underlying repository.
		/// </summary>
		ValueTask<IReadOnlyList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default);

		/// <summary>
		/// Counts the entities matching the given filter, forwarding to
		/// the protected <c>CountAsync(IQueryFilter, CancellationToken)</c>
		/// entry point of the underlying repository.
		/// </summary>
		ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default);

		/// <summary>
		/// Determines whether any entity matches the given filter, forwarding
		/// to the protected <c>ExistsAsync(IQueryFilter, CancellationToken)</c>
		/// entry point of the underlying repository.
		/// </summary>
		ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the underlying <see cref="IQueryable{TEntity}"/> that backs
		/// the entity set, forwarding to the protected <c>Queryable()</c>
		/// hatch of the underlying repository.
		/// </summary>
		IQueryable<TEntity> Queryable();
	}

	/// <summary>
	/// A single-generic convenience form of
	/// <see cref="ITestRepository{TEntity, TKey}"/> for the no-key
	/// <see cref="Repository{TEntity}"/> base class, defaulting the key
	/// type to <see cref="object"/>.
	/// </summary>
	/// <typeparam name="TEntity">The type of entity handled by the repository.</typeparam>
	public interface ITestRepository<TEntity> : ITestRepository<TEntity, object> where TEntity : class {
	}
}