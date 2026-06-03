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

using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// An object that maps a field name to a <see cref="Expression"/>
	/// used to select the field from an entity.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to map fields for.
	/// </typeparam>
	public interface IFieldMapper<TEntity> {
		/// <summary>
		/// Maps the given field name to an expression that selects
		/// a member of the entity.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to map.
		/// </param>
		/// <returns>
		/// Returns an expression that selects the field from the entity.
		/// </returns>
		Expression<Func<TEntity, object?>> MapField(string fieldName);
	}
}
