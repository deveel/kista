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

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Kista {
	/// <summary>
	/// An object that combines multiple <see cref="IQueryOrder"/> rules
	/// to be applied to a query.
	/// </summary>
	public sealed class CombinedOrder : IQueryOrder, IEnumerable<IQueryOrder> {
		private readonly IList<IQueryOrder> sorts;

		/// <summary>
		/// Constructs the combined sort with the given list of rules.
		/// </summary>
		/// <param name="sorts">
		/// The initial list of sorts to combine.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given list of sorts is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if the given list of sorts is empty.
		/// </exception>
		public CombinedOrder(IEnumerable<IQueryOrder> sorts) {
			ArgumentNullException.ThrowIfNull(sorts, nameof(sorts));

			var list = sorts.ToList();

			if (list.Count == 0)
				throw new ArgumentException("The list of sorts cannot be empty", nameof(sorts));

			this.sorts = list;
		}

		IEnumerator<IQueryOrder> IEnumerable<IQueryOrder>.GetEnumerator() => sorts.GetEnumerator();

		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<IQueryOrder>).GetEnumerator();

		/// <summary>
		/// Combines this list of sorts with the given one.
		/// </summary>
		/// <param name="sort">
		/// The sort rule to add to the combination.
		/// </param>
		/// <returns>
		/// Returns a new <see cref="CombinedOrder"/> that combines
		/// the rules of this instance with the given one.
		/// </returns>
		public CombinedOrder Combine(IQueryOrder sort) {
			ArgumentNullException.ThrowIfNull(sort, nameof(sort));

			var list = new List<IQueryOrder>(sorts);
			if (sort is CombinedOrder combinedSort) {
				list.AddRange(combinedSort.sorts);
			} else {
				list.Add(sort);
			}

			return new CombinedOrder(list);
		}
	}
}
