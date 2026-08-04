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

namespace Kista.Events {
	/// <summary>
	/// The single-key variant of
	/// <see cref="EntityEventInterceptor{TEntity, TKey}"/>, used by
	/// <see cref="EntityManager{TEntity}"/> (which uses <c>object</c> as
	/// the key type). See the two-arg class for the full contract
	/// documentation.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the pipeline.
	/// </typeparam>
	/// <remarks>
	/// This class implements both
	/// <see cref="IEntityManagerInterceptor{TEntity}"/> (the single-key
	/// interface resolved by <see cref="EntityManager{TEntity}"/>) and,
	/// through inheritance, <see cref="IEntityManagerInterceptor{TEntity, TKey}"/>
	/// with <c>object</c> as the key type — so it participates in both
	/// the single-key and the two-key pipelines.
	/// </remarks>
	/// <seealso cref="EntityEventInterceptor{TEntity, TKey}"/>
	public class EntityEventInterceptor<TEntity> : EntityEventInterceptor<TEntity, object>, IEntityManagerInterceptor<TEntity>
		where TEntity : class {
		/// <summary>
		/// Constructs the single-key interceptor with the event publisher
		/// and an optional logger.
		/// </summary>
		/// <param name="publisher">
		/// The publisher used to dispatch the event payloads.
		/// </param>
		/// <param name="logger">
		/// The logger used to report publish failures.
		/// </param>
		public EntityEventInterceptor(
			IEntityEventPublisher<TEntity> publisher,
			Microsoft.Extensions.Logging.ILogger? logger = null)
			: base(publisher, logger) {
		}
	}
}