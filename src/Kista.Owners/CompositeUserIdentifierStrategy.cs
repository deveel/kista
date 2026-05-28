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
	/// A composite strategy that evaluates multiple user identifier strategies in order,
	/// returning the first non-null result (fallback chain pattern).
	/// </summary>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public class CompositeUserIdentifierStrategy<TKey> : IUserIdentifierStrategy<TKey>
	{
		private readonly List<IUserIdentifierStrategy<TKey>> strategies = new();

		/// <summary>
		/// Gets the registered strategies in order.
		/// </summary>
		public IReadOnlyList<IUserIdentifierStrategy<TKey>> Strategies => strategies;

		/// <summary>
		/// Adds a strategy to the end of the chain.
		/// </summary>
		/// <param name="strategy">The strategy to add.</param>
		/// <returns>This instance for fluent chaining.</returns>
		public CompositeUserIdentifierStrategy<TKey> Add(IUserIdentifierStrategy<TKey> strategy)
		{
			ArgumentNullException.ThrowIfNull(strategy);
			strategies.Add(strategy);
			return this;
		}

		/// <inheritdoc/>
		public TKey? GetUserId(IServiceProvider? serviceProvider = null)
		{
			foreach (var strategy in strategies)
			{
				var userId = strategy.GetUserId(serviceProvider);
				if (userId != null)
					return userId;
			}

			return default;
		}
	}

	/// <summary>
	/// Fluent builder interface for configuring a composite user identifier strategy.
	/// </summary>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public interface IUserIdentifierStrategyBuilder<TKey>
	{
		/// <summary>
		/// Adds a custom strategy to the chain.
		/// </summary>
		/// <param name="strategy">The strategy instance to add.</param>
		/// <returns>The builder for fluent chaining.</returns>
		IUserIdentifierStrategyBuilder<TKey> Add(IUserIdentifierStrategy<TKey> strategy);

		/// <summary>
		/// Adds a static strategy that returns a fixed user identifier.
		/// </summary>
		/// <param name="userId">The fixed user identifier.</param>
		/// <returns>The builder for fluent chaining.</returns>
		IUserIdentifierStrategyBuilder<TKey> AddStatic(TKey userId);
	}

	/// <summary>
	/// Implementation of <see cref="IUserIdentifierStrategyBuilder{TKey}"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public class UserIdentifierStrategyBuilder<TKey> : IUserIdentifierStrategyBuilder<TKey>
	{
		private readonly CompositeUserIdentifierStrategy<TKey> composite = new();

		/// <summary>
		/// Builds and returns the composite strategy.
		/// </summary>
		public CompositeUserIdentifierStrategy<TKey> Build() => composite;

		/// <inheritdoc/>
		public IUserIdentifierStrategyBuilder<TKey> Add(IUserIdentifierStrategy<TKey> strategy)
		{
			composite.Add(strategy);
			return this;
		}

		/// <inheritdoc/>
		public IUserIdentifierStrategyBuilder<TKey> AddStatic(TKey userId)
		{
			composite.Add(new StaticUserIdentifierStrategy<TKey>(userId));
			return this;
		}
	}
}
