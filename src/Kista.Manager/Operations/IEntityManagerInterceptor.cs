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

using Deveel;

namespace Kista {
	/// <summary>
	/// An interceptor that participates in the operation pipeline of
	/// an <see cref="EntityManager{TEntity, TKey}"/>, providing cross-cutting
	/// concerns (such as audit, event emission, tracing, multi-tenancy)
	/// with a single ordered, testable extension point.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the pipeline.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key identifying the entity.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// Interceptors are resolved lazily from DI through
	/// <c>IEnumerable&lt;IEntityManagerInterceptor&lt;TEntity, TKey&gt;&gt;</c>
	/// and run in registration order. When no interceptor is registered,
	/// the pipeline has zero cost.
	/// </para>
	/// <para>
	/// <see cref="PreWriteAsync"/> is invoked before the repository write:
	/// it may transform the entity (through
	/// <see cref="IEntityOperationContext{TEntity, TKey}.Entity"/>) or
	/// short-circuit the operation by returning a failed
	/// <see cref="OperationResult"/>. Returning <c>null</c> continues
	/// the chain.
	/// </para>
	/// <para>
	/// <see cref="PostWriteAsync"/> is invoked only after a successful
	/// write (or a successful short-circuit): it is not called when the
	/// repository write throws, nor when a previous interceptor
	/// short-circuits the chain with a failed result.
	/// </para>
	/// <para>
	/// The existing <c>On*Async</c> virtual hooks on
	/// <see cref="EntityManager{TEntity, TKey}"/> are preserved: a builtin
	/// interceptor wraps them and runs as the last interceptor in the
	/// chain, so subclass overrides keep working and coexist with
	/// user-registered interceptors.
	/// </para>
	/// </remarks>
	public interface IEntityManagerInterceptor<TEntity, TKey>
		where TEntity : class
		where TKey : notnull {
		/// <summary>
		/// Invoked before the repository write is performed, for each
		/// interceptor in registration order.
		/// </summary>
		/// <param name="context">
		/// The context carrying the operation kind, the mutable entity,
		/// the original pre-image (for update/remove/restore/hard-delete),
		/// the key, the actor, the timestamp, the cancellation token,
		/// and a per-operation items bag.
		/// </param>
		/// <returns>
		/// Returns <c>null</c> to continue the chain and proceed with
		/// the repository write, or a failed <see cref="IOperationResult"/>
		/// to short-circuit the chain and skip the repository write and
		/// all downstream interceptors.
		/// </returns>
		ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context);

		/// <summary>
		/// Invoked after a successful repository write, for each
		/// interceptor in registration order.
		/// </summary>
		/// <param name="context">
		/// The context carrying the operation information. The
		/// <see cref="IEntityOperationContext{TEntity, TKey}.Entity"/>
		/// property reflects the entity as persisted.
		/// </param>
		/// <param name="result">
		/// The result of the operation (success or not-changed).
		/// </param>
		/// <returns>
		/// Returns a <see cref="ValueTask"/> that completes when the
		/// post-write side effects have been applied.
		/// </returns>
		ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result);
	}
}