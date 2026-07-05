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
	}
}