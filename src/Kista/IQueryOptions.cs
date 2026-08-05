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
	/// Defines a bag of query-level options that influence how a query
	/// is executed by a repository driver.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Query options are carried alongside the filter and order of an
	/// <see cref="IQuery"/> and are consulted by the driver-specific
	/// query pipeline. The canonical use case is the selection of a
	/// <see cref="SoftDeleteMode"/> to include or isolate soft-deleted
	/// entities in the results.
	/// </para>
	/// <para>
	/// A <c>null</c> options bag is equivalent to
	/// <see cref="QueryOptions.Default"/>.
	/// </para>
	/// </remarks>
	public interface IQueryOptions {
		/// <summary>
		/// Gets the mode controlling how soft-deleted entities are
		/// treated by the query.
		/// </summary>
		SoftDeleteMode SoftDeleteMode { get; }

		/// <summary>
		/// Gets a value indicating whether the query should return tracked
		/// entities (change-tracking enabled) or non-tracked entities
		/// (<c>AsNoTracking</c>).
		/// </summary>
		/// <remarks>
		/// <para>
		/// The default is <c>false</c> (non-tracked): read operations
		/// (<c>FindFirst</c>, <c>FindAll</c>, <c>GetPage</c>) do not register
		/// returned entities in the EF Core <c>ChangeTracker</c>, avoiding
		/// memory bloat and slower <c>SaveChanges</c> on read-only paths.
		/// </para>
		/// <para>
		/// Set to <c>true</c> only when you intend to mutate the returned
		/// entities and call <c>UpdateAsync</c> on the same
		/// <c>DbContext</c> scope.
		/// </para>
		/// <para>
		/// This option is only consulted by EF Core-based drivers
		/// (<c>Kista.EntityFramework</c>). Other drivers ignore it.
		/// </para>
		/// </remarks>
		bool TrackEntities { get; }
	}
}