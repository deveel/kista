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

namespace Kista
{
	/// <summary>
	/// A service that provides information about the current user
	/// of the application.
	/// </summary>
	/// <remarks>
	/// This contact can be used to retrieve identifier about the
	/// user that is currently using the application, such as the
	/// username or the user ID.
	/// </remarks>
	/// <typeparam name="TKey">
	/// The type of the key that identifies the user.
	/// </typeparam>
	public interface IUserAccessor<TKey>
	{
		/// <summary>
		/// Gets the identifier of the current user.
		/// </summary>
		/// <returns>
		/// Returns a string that represents the identifier of the
		/// user that is currently using the application.
		/// </returns>
		TKey? GetUserId();
	}
}
