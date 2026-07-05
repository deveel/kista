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
	/// A default, immutable implementation of <see cref="IQueryOptions"/>.
	/// </summary>
	public sealed class QueryOptions : IQueryOptions {
		/// <summary>
		/// Gets the default query options, where
		/// <see cref="IQueryOptions.SoftDeleteMode"/> is
		/// <see cref="SoftDeleteMode.Default"/>.
		/// </summary>
		public static IQueryOptions Default { get; } = new QueryOptions(SoftDeleteMode.Default);

		private QueryOptions(SoftDeleteMode softDeleteMode) {
			SoftDeleteMode = softDeleteMode;
		}

		/// <inheritdoc />
		public SoftDeleteMode SoftDeleteMode { get; }

		/// <summary>
		/// Returns a new <see cref="IQueryOptions"/> with the given
		/// soft-delete mode.
		/// </summary>
		/// <param name="mode">
		/// The soft-delete mode to apply.
		/// </param>
		/// <returns>
		/// Returns a new <see cref="IQueryOptions"/> carrying the given mode.
		/// </returns>
		public static IQueryOptions WithSoftDeleteMode(SoftDeleteMode mode) => new QueryOptions(mode);
	}
}