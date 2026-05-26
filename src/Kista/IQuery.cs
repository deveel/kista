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
	/// Defines the contract for a query that can 
	/// be executed against a repository.
	/// </summary>
	public interface IQuery {
		/// <summary>
		/// Gets the filter that is applied to the query
		/// to select the entities.
		/// </summary>
		IQueryFilter? Filter { get; }

		/// <summary>
		/// Gets the sort that is applied to the query
		/// to order the entities.
		/// </summary>
		IQueryOrder? Order { get; }
	}
}
