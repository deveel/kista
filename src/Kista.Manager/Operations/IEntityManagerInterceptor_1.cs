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
	/// an <see cref="EntityManager{TEntity}"/>, providing cross-cutting
	/// concerns (such as audit, event emission, tracing, multi-tenancy)
	/// with a single ordered, testable extension point.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the pipeline.
	/// </typeparam>
	/// <remarks>
	/// This is the single-key variant of
	/// <see cref="IEntityManagerInterceptor{TEntity, TKey}"/>, used by
	/// <see cref="EntityManager{TEntity}"/> (which uses <c>object</c>
	/// as the key type). See the two-arg interface for the full
	/// contract documentation.
	/// </remarks>
	/// <seealso cref="IEntityManagerInterceptor{TEntity, TKey}"/>
	public interface IEntityManagerInterceptor<TEntity>
		where TEntity : class {
		/// <inheritdoc cref="IEntityManagerInterceptor{TEntity, TKey}.PreWriteAsync"/>
		ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, object> context);

		/// <inheritdoc cref="IEntityManagerInterceptor{TEntity, TKey}.PostWriteAsync"/>
		ValueTask PostWriteAsync(IEntityOperationContext<TEntity, object> context, IOperationResult result);
	}
}