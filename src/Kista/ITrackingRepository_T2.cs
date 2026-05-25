// Copyright 2023-2025 Antonello Provenzano
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
	/// Defines a repository that is able to track changes 
	/// on entities returned by queries.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity managed by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key used to identify the entity.
	/// </typeparam>
	public interface ITrackingRepository<TEntity, TKey> : IRepository<TEntity, TKey>
		where TEntity : class {
		/// <summary>
		/// Gets a value indicating if the repository is tracking
		/// changes on entities.
		/// </summary>
		/// <remarks>
		/// A repository implementation of this contract might
		/// be configured not to track changes on entities, in
		/// this case the value of this property is <c>false</c>.
		/// </remarks>
		bool IsTrackingChanges { get; }

		/// <summary>
		/// Finds the original entity that is being tracked by the repository,
		/// as it was loaded from the data source.
		/// </summary>
		/// <param name="key">
		/// The key that identifies the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the original version of entity that is being tracked by the repository,
		/// as it was loaded from the data source, or <c>null</c> if the entity is not
		/// found or it's not being tracked.
		/// </returns>
		ValueTask<TEntity?> FindOriginalAsync(TKey key, CancellationToken cancellationToken = default);
	}
}
