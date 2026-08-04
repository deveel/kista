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

using System.Linq.Dynamic.Core;

namespace Kista {
	/// <summary>
	/// A hardened <see cref="ParsingConfig"/> used by Kista when parsing Dynamic LINQ
	/// expression strings, designed to reduce the risk of remote code execution when
	/// expression strings originate from untrusted input (e.g. API clients).
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Security note.</b> The default <see cref="ParsingConfig.Default"/> permits the
	/// <c>new</c> operator and fully-qualified type casts, which allows an attacker who
	/// controls the expression string to instantiate arbitrary types
	/// (e.g. <c>new System.Diagnostics.Process()</c>) or cast to them, leading to remote
	/// code execution. <see cref="KistaParsingConfig"/> closes these vectors by:
	/// </para>
	/// <list type="bullet">
	///   <item><c>DisallowNewKeyword = true</c> — blocks the <c>new</c> operator entirely.</item>
	///   <item><c>SupportCastingToFullyQualifiedTypeAsString = false</c> — blocks fully-qualified type casts.</item>
	/// </list>
	/// <para>
	/// Static method calls to arbitrary types (e.g. <c>System.IO.File.Exists(...)</c>) are
	/// blocked by the default <c>IDynamicLinqCustomTypeProvider</c>, which restricts type
	/// resolution to the <c>System</c> namespace enums and a small set of primitives.
	/// </para>
	/// <para>
	/// <b>This is defense-in-depth, not a complete sandbox.</b> If your expression strings
	/// come from untrusted input, you must additionally apply an application-level allow-list
	/// of permitted fields and operators before passing the string to
	/// <see cref="FilterExpression"/> or <see cref="DynamicLinqFilter"/>. Never accept raw
	/// expression strings from clients without validation.
	/// </para>
	/// </remarks>
	public static class KistaParsingConfig {
		private static readonly ParsingConfig _instance = CreateHardenedConfig();

		/// <summary>
		/// Gets the shared hardened <see cref="ParsingConfig"/> instance used by Kista.
		/// </summary>
		/// <value>
		/// A <see cref="ParsingConfig"/> with <see cref="ParsingConfig.DisallowNewKeyword"/>
		/// set to <c>true</c> and <see cref="ParsingConfig.SupportCastingToFullyQualifiedTypeAsString"/>
		/// set to <c>false</c>.
		/// </value>
		public static ParsingConfig Instance => _instance;

		private static ParsingConfig CreateHardenedConfig() {
			var config = new ParsingConfig {
				// Block the `new` operator — prevents instantiation of arbitrary types
				// (e.g. `new System.Diagnostics.Process()`).
				DisallowNewKeyword = true,

				// Block fully-qualified type casts — prevents casting to arbitrary types
				// by their fully-qualified string name.
				SupportCastingToFullyQualifiedTypeAsString = false,
			};

			return config;
		}
	}
}