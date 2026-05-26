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
	/// A sorting rule that references a field
	/// by its name.
	/// </summary>
	public sealed class FieldOrder : IQueryOrder, IDirectionalOrder {
		/// <summary>
		/// Constructs the sorting rule for the given
		/// field name and direction.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by.
		/// </param>
		/// <param name="direction">
		/// The direction of the sorting.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given <paramref name="fieldName"/> is <c>null</c>.
		/// </exception>
		public FieldOrder(string fieldName, SortDirection direction = SortDirection.Ascending) {
			ArgumentNullException.ThrowIfNull(fieldName, nameof(fieldName));

			FieldName = fieldName;
			Direction = direction;
		}

		/// <summary>
		/// Gets the name of the field to sort by.
		/// </summary>
		public string FieldName { get; }

		/// <summary>
		/// Gets the direction of the sorting.
		/// </summary>
		public SortDirection Direction { get; }

		/// <summary>
		/// Resolves the field name to an expression
		/// that selects the field to sort.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity of a collection that is the target
		/// of the sorting and that defines the field to select.
		/// </typeparam>
		/// <param name="mapper">
		/// An oobject that maps the field name to an expression.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="ExpressionSort{TEntity}"/>
		/// that is the result of the mapping.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the given <paramref name="mapper"/> cannot map
		/// the field name to an expression that selects the field.
		/// </exception>
		public ExpressionSort<TEntity> Map<TEntity>(IFieldMapper<TEntity> mapper) where TEntity : class {
			var field = mapper.MapField(FieldName);
			if (field == null)
				throw new InvalidOperationException($"The field '{FieldName}' cannot be mapped to an expression");

			return new ExpressionSort<TEntity>(field, Direction);
		}
	}
}
