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
	/// Handles lifecycle operations (existence check, create, drop, seed)
	/// for a specific entity type.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity managed by the handler.</typeparam>
#pragma warning disable S2326 // Type parameter is used for type-safe DI registration even though not referenced in method signatures
	public interface IRepositoryLifecycleHandler<TEntity> where TEntity : class {
		/// <summary>
		/// Checks whether the repository for the entity type exists.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><c>true</c> if the repository exists; otherwise <c>false</c>.</returns>
		ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates the repository for the entity type.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask CreateAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Drops the repository for the entity type.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask DropAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Seeds the repository with the given data.
		/// </summary>
		/// <param name="seedData">Optional seed data to insert.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default);
	}
#pragma warning restore S2326
}
