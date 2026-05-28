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
	/// A user identifier strategy that returns a fixed, pre-configured user identifier.
	/// </summary>
	/// <remarks>
	/// This strategy is useful for background jobs, system operations, or disconnected
	/// scenarios where the user context is not available through HTTP or other means.
	/// </remarks>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public class StaticUserIdentifierStrategy<TKey> : IUserIdentifierStrategy<TKey>
	{
		private readonly TKey userId;

		/// <summary>
		/// Initializes a new instance with the specified user identifier.
		/// </summary>
		/// <param name="userId">The fixed user identifier to return.</param>
		public StaticUserIdentifierStrategy(TKey userId)
		{
			this.userId = userId;
		}

		/// <inheritdoc/>
		public TKey? GetUserId(IServiceProvider? serviceProvider = null) => userId;
	}
}
