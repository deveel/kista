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
	/// A contract for an entity that supports logical (soft) deletion,
	/// where the record is flagged as deleted instead of being physically
	/// removed from the repository.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When an entity implements this interface, the repository infrastructure
	/// transparently excludes deleted records from regular queries and rewrites
	/// <see cref="IRepository{TEntity, TKey}.RemoveAsync(TEntity, CancellationToken)"/>
	/// into a soft-delete update, unless the caller explicitly requests
	/// hard deletion through
	/// <see cref="IRepository{TEntity, TKey}.HardDeleteAsync(TEntity, CancellationToken)"/>.
	/// </para>
	/// <para>
	/// The <see cref="IsDeleted"/> flag is the authoritative indicator of the
	/// deletion state, while <see cref="DeletedAtUtc"/> and <see cref="DeletedBy"/>
	/// carry optional audit metadata about when and by whom the record was retired.
	/// </para>
	/// </remarks>
	public interface ISoftDeletable {
		/// <summary>
		/// Gets or sets a value indicating whether the entity has been
		/// logically deleted.
		/// </summary>
		bool IsDeleted { get; set; }

		/// <summary>
		/// Gets or sets the UTC timestamp at which the entity was soft-deleted,
		/// or <c>null</c> if the entity has not been deleted.
		/// </summary>
		DateTimeOffset? DeletedAtUtc { get; set; }

		/// <summary>
		/// Gets or sets an optional identifier of the actor that performed
		/// the soft-deletion, or <c>null</c> if no actor was recorded.
		/// </summary>
		string? DeletedBy { get; set; }
	}
}