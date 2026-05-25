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

using System;

namespace Kista {
	/// <summary>
	/// A repository whose lifecycle can be controlled by the user
	/// </summary>
	public interface IControllableRepository {
		/// <summary>
		/// Checks if the repository exists in the underlying storage
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the repository exists, or <c>false</c>
		/// </returns>
		ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates the repository in the underlying storage
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a <see cref="Task"/> that completes when the repository
		/// is created in the underlying storage
		/// </returns>
		ValueTask CreateAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Drops the repository from the underlying storage
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a <see cref="Task"/> that completes when the repository
		/// is dropped from the underlying storage
		/// </returns>
		ValueTask DropAsync(CancellationToken cancellationToken = default);
	}
}
