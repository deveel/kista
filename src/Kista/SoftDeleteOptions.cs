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
	/// Provides configuration knobs for the soft-delete behaviour
	/// registered through <c>WithSoftDelete</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// In v1.7.0 the options bag is intentionally minimal: soft-delete
	/// filtering activates automatically for any entity implementing
	/// <see cref="ISoftDeletable"/>. The options object is reserved for
	/// future extensions (for example a <c>DeletedBy</c> accessor) and
	/// can be ignored by consumers.
	/// </para>
	/// </remarks>
	public sealed class SoftDeleteOptions {
		/// <summary>
		/// Gets the default soft-delete options.
		/// </summary>
		public static SoftDeleteOptions Default { get; } = new SoftDeleteOptions();
	}
}