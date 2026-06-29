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

namespace Kista {
	/// <summary>
	/// An obsolete adapter that bridges the legacy <see cref="IRepositoryController"/>
	/// interface to the modern <see cref="IRepositoryLifecycleService"/>.
	/// Use <see cref="IRepositoryLifecycleService"/> directly instead.
	/// </summary>
	[Obsolete("Use IRepositoryLifecycleService instead")]
	[ExcludeFromCodeCoverage]
	public class RepositoryControllerAdapter : IRepositoryController {
		private readonly IRepositoryLifecycleService service;

		/// <summary>
		/// Creates a new adapter wrapping the given lifecycle service.
		/// </summary>
		/// <param name="service">The lifecycle service to delegate to.</param>
		public RepositoryControllerAdapter(IRepositoryLifecycleService service) {
			this.service = service;
		}

		/// <inheritdoc/>
		public ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.CreateRepositoryAsync<TEntity>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.CreateRepositoryAsync<TEntity, TKey>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.DropRepositoryAsync<TEntity>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.DropRepositoryAsync<TEntity, TKey>(cancellationToken);
	}
}
