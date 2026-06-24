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
	/// Defines a specification that encapsulates a business rule
	/// and produces a driver-agnostic <see cref="IQuery"/> for
	/// querying a repository.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity the specification applies to.
	/// </typeparam>
	public interface ISpecification<TEntity> where TEntity : class {
		/// <summary>
		/// Converts the specification to a driver-agnostic <see cref="IQuery"/>
		/// that can be executed against any repository.
		/// </summary>
		/// <returns>
		/// Returns an <see cref="IQuery"/> representing the specification's
		/// filtering and sorting criteria.
		/// </returns>
		IQuery ToQuery();
	}
}
