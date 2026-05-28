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
	/// An implementation of <see cref="IUserAccessor{TKey}"/> that delegates
	/// user resolution to a composite strategy chain.
	/// </summary>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public class StrategyBasedUserAccessor<TKey> : IUserAccessor<TKey>
	{
		private readonly CompositeUserIdentifierStrategy<TKey> compositeStrategy;
		private readonly IServiceProvider? serviceProvider;

		/// <summary>
		/// Initializes a new instance with the specified composite strategy.
		/// </summary>
		/// <param name="compositeStrategy">The composite strategy to use for user resolution.</param>
		/// <param name="serviceProvider">Optional service provider for strategy resolution.</param>
		public StrategyBasedUserAccessor(
			CompositeUserIdentifierStrategy<TKey> compositeStrategy,
			IServiceProvider? serviceProvider = null)
		{
			ArgumentNullException.ThrowIfNull(compositeStrategy);

			this.compositeStrategy = compositeStrategy;
			this.serviceProvider = serviceProvider;
		}

		/// <inheritdoc/>
		public TKey? GetUserId() => compositeStrategy.GetUserId(serviceProvider);
	}
}
