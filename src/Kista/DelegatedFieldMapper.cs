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
	/// An implementation of <see cref="IFieldMapper{TEntity}"/> that
	/// delegates the mapping of a field to an external function.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to map the fields to.
	/// </typeparam>
	public sealed class DelegatedFieldMapper<TEntity> : IFieldMapper<TEntity> {
		private readonly Func<string, Expression<Func<TEntity, object?>>> fieldMapper;

		/// <summary>
		/// Creates a new instance of the mapper that delegates the mapping
		/// of a field to the given function.
		/// </summary>
		/// <param name="fieldMapper">
		/// The external function that maps a field name to an expression
		/// that selects the field from the entity.
		/// </param>
		public DelegatedFieldMapper(Func<string, Expression<Func<TEntity, object?>>> fieldMapper) {
			ArgumentNullException.ThrowIfNull(fieldMapper);

			this.fieldMapper = fieldMapper;
		}

		/// <inheritdoc/>
		public Expression<Func<TEntity, object?>> MapField(string fieldName) {
			return fieldMapper(fieldName);
		}
	}
}
