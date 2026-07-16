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
	/// Adapts a single-key <see cref="IEntityManagerInterceptor{TEntity}"/>
	/// to the two-key <see cref="IEntityManagerInterceptor{TEntity, TKey}"/>
	/// contract, using <c>object</c> as the key type.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the pipeline.
	/// </typeparam>
	/// <remarks>
	/// This adapter is used internally by <see cref="EntityManager{TEntity}"/>
	/// to wrap single-key interceptors resolved from DI and feed them
	/// into the two-key pipeline, mirroring the
	/// <c>EntityValidatorWrapper</c> pattern used for validators.
	/// </remarks>
	internal sealed class EntityManagerInterceptorWrapper<TEntity> : IEntityManagerInterceptor<TEntity, object>
		where TEntity : class {
		private readonly IEntityManagerInterceptor<TEntity> _interceptor;

		/// <summary>
		/// Constructs the adapter around the given single-key interceptor.
		/// </summary>
		/// <param name="interceptor">
		/// The single-key interceptor to adapt.
		/// </param>
		public EntityManagerInterceptorWrapper(IEntityManagerInterceptor<TEntity> interceptor) {
			_interceptor = interceptor;
		}

		/// <inheritdoc/>
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, object> context)
			=> _interceptor.PreWriteAsync(context);

		/// <inheritdoc/>
		public ValueTask PostWriteAsync(IEntityOperationContext<TEntity, object> context, IOperationResult result)
			=> _interceptor.PostWriteAsync(context, result);
	}
}