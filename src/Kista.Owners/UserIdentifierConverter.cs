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

using System.ComponentModel;

namespace Kista {
	/// <summary>
	/// A helper class that converts string values to the type of the user
	/// identifier key, used by the user identifier strategies to avoid
	/// duplicating the conversion logic across implementations.
	/// </summary>
	public static class UserIdentifierConverter {
		/// <summary>
		/// Converts the given string value to the type of the user identifier key.
		/// </summary>
		/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
		/// <param name="value">The string value to convert.</param>
		/// <returns>
		/// The converted value, or the default of <typeparamref name="TKey"/>
		/// when the conversion fails.
		/// </returns>
		public static TKey? Convert<TKey>(string value) {
			try {
				if (typeof(TKey) == typeof(string))
					return (TKey)(object)value;

				var converter = TypeDescriptor.GetConverter(typeof(TKey));
				return (TKey?)converter.ConvertFromInvariantString(value);
			} catch (FormatException) {
				return default;
			} catch (NotSupportedException) {
				return default;
			} catch (ArgumentException) {
				return default;
			} catch (InvalidCastException) {
				return default;
			}
		}
	}
}