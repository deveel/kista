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

namespace Kista
{
	/// <summary>
	/// Configuration options for the <see cref="UserScopedRepositoryDecorator{TEntity, TKey, TUserKey}"/>.
	/// </summary>
	public class UserScopingOptions
	{
		/// <summary>
		/// Gets or sets whether to throw an <see cref="System.InvalidOperationException"/>
		/// when no user context is available. Defaults to <c>true</c>.
		/// </summary>
		/// <remarks>
		/// When <c>false</c>, operations return empty results or <c>null</c> instead of throwing.
		/// </remarks>
		public bool ThrowWhenUserNotSet { get; set; }

		/// <summary>
		/// Gets or sets the name of the owner property to use, overriding automatic discovery.
		/// </summary>
		/// <remarks>
		/// When <c>null</c> (default), the decorator discovers the owner property by scanning
		/// for the <see cref="DataOwnerAttribute"/> attribute, then falling back to a property
		/// named <c>"Owner"</c>.
		/// </remarks>
		public string? OwnerPropertyName { get; set; }
	}
}
