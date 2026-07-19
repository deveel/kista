// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;

namespace Kista {
	/// <summary>
	/// A helper class that provides common comparisons for primary key values,
	/// to reduce code duplication across repository implementations.
	/// </summary>
	public static class KeyHelper {
		/// <summary>
		/// Determines whether the given key is <c>null</c> for reference types
		/// or has no value for nullable value types, using the same semantics
		/// as a <c>key == null</c> comparison without triggering the S2955
		/// rule on unconstrained generic type parameters.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <param name="key">The key to test against <c>null</c>.</param>
		/// <returns>
		/// <c>true</c> if <paramref name="key"/> is <c>null</c>; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsNull<TKey>([NotNullWhen(false)] TKey? key)
			=> key is null;
	}
}