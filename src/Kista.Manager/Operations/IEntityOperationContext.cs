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
	/// Carries the contextual information of a single write operation
	/// flowing through the <see cref="EntityManager{TEntity, TKey}"/>
	/// operation pipeline.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity targeted by the operation.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key identifying the entity.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// A new instance of this context is created for each write
	/// operation (and for each entity in a range operation) and is
	/// shared across all the interceptors registered in the pipeline.
	/// </para>
	/// <para>
	/// The <see cref="Entity"/> property is mutable: interceptors
	/// running in <see cref="IEntityManagerInterceptor{TEntity, TKey}.PreWriteAsync"/>
	/// can transform the entity before it is persisted. The
	/// <see cref="Original"/> property carries the pre-image of the
	/// entity (the value loaded from the repository) for update,
	/// remove, restore and hard-delete operations, and is <c>null</c>
	/// for create operations.
	/// </para>
	/// <para>
	/// The <see cref="Items"/> bag is an arbitrary key/value store
	/// that interceptors can use to share data between pre-write and
	/// post-write steps, or across interceptors in the chain.
	/// </para>
	/// </remarks>
	public interface IEntityOperationContext<TEntity, TKey>
		where TEntity : class
		where TKey : notnull {
		/// <summary>
		/// Gets the kind of the operation being performed.
		/// </summary>
		EntityOperationKind Kind { get; }

		/// <summary>
		/// Gets the entity targeted by the operation.
		/// </summary>
		/// <remarks>
		/// The instance returned by this property is mutable:
		/// interceptors can modify it during
		/// <see cref="IEntityManagerInterceptor{TEntity, TKey}.PreWriteAsync"/>
		/// to transform the entity before it is persisted. The
		/// mutated instance is the one forwarded to the repository.
		/// </remarks>
		TEntity Entity { get; set; }

		/// <summary>
		/// Gets the pre-image of the entity loaded from the repository,
		/// for update, remove, restore and hard-delete operations.
		/// </summary>
		/// <remarks>
		/// This property is <c>null</c> for create operations, where
		/// no entity is loaded from the repository before the write.
		/// </remarks>
		TEntity? Original { get; }

		/// <summary>
		/// Gets the key identifying the entity targeted by the operation,
		/// or <c>null</c> if the entity does not have a valid key.
		/// </summary>
		TKey? Key { get; }

		/// <summary>
		/// Gets the identifier of the actor that initiated the operation,
		/// resolved from the <see cref="IUserAccessor{TKey}"/> service,
		/// or <c>null</c> if no user accessor is registered.
		/// </summary>
		string? Actor { get; }

		/// <summary>
		/// Gets the timestamp at which the operation was started,
		/// resolved from the <see cref="ISystemTime"/> service.
		/// </summary>
		DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Gets the cancellation token associated with the operation.
		/// </summary>
		CancellationToken CancellationToken { get; }

		/// <summary>
		/// Gets a per-operation key/value bag that interceptors can
		/// use to share data between pre-write and post-write steps,
		/// or across interceptors in the chain.
		/// </summary>
		IDictionary<string, object?> Items { get; }
	}
}