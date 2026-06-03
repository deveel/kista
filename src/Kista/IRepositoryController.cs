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

namespace Kista {
	/// <summary>
	/// A service used to control the lifecycle of the repositories
	/// </summary>
	public interface IRepositoryController {
		/// <summary>
		/// Creates a repository for the given entity type
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity managed by the repository
		/// </typeparam>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a <see cref="Task"/> that can be used to await the operation
		/// </returns>
		ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Creates a repository for the given entity type
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity managed by the repository
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a <see cref="ValueTask"/> that can be used to await the operation
		/// </returns>
		ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Drops the repository for the given entity type
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity managed by the repository
		/// </typeparam>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a <see cref="ValueTask"/> that can be used to await the operation
		/// </returns>
		ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Drops the repository for the given entity type
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity managed by the repository
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity
		/// </typeparam>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a <see cref="ValueTask"/> that can be used to await the operation
		/// </returns>
		ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default)
			where TEntity : class;
	}
}
