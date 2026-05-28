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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista
{
	/// <summary>
	/// Provides extension methods for the <see cref="IServiceCollection"/> interface
	/// to register user accessor services.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Registers a singleton <see cref="IUserAccessor{TKey}"/> service
		/// that resolves the current user identifier using a chain of configurable strategies.
		/// </summary>
		/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
		/// <param name="services">The collection of services to register the user accessor.</param>
		/// <param name="configure">
		/// A delegate to configure the strategy chain. Strategies are evaluated in registration order;
		/// the first non-null result is returned.
		/// </param>
		/// <returns>The given collection of services for chaining calls.</returns>
		public static IServiceCollection AddUserAccessor<TKey>(
			this IServiceCollection services,
			Action<IUserIdentifierStrategyBuilder<TKey>> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			services.AddHttpContextAccessor();

			var builder = new UserIdentifierStrategyBuilder<TKey>();
			configure(builder);

			var composite = builder.Build();
			services.AddSingleton(composite);
			services.AddSingleton<IUserAccessor<TKey>, StrategyBasedUserAccessor<TKey>>();

			return services;
		}

		/// <summary>
		/// Registers HTTP-based strategies for <see cref="IUserAccessor{TKey}"/> with default configuration:
		/// claim ("sub") → query string ("user_id") → route value ("userId").
		/// </summary>
		/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
		/// <param name="services">The collection of services to register the user accessor.</param>
		/// <returns>The given collection of services for chaining calls.</returns>
		public static IServiceCollection AddHttpUserAccessor<TKey>(this IServiceCollection services) {
			return services.AddUserAccessor<TKey>(builder => {
				builder.Add(new ClaimUserIdentifierStrategy<TKey>());
				builder.Add(new QueryStringUserIdentifierStrategy<TKey>());
				builder.Add(new RouteUserIdentifierStrategy<TKey>());
			});
		}

		/// <summary>
		/// Registers HTTP-based strategies for <see cref="IUserAccessor{TKey}"/> with custom configuration.
		/// </summary>
		/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
		/// <param name="services">The collection of services to register the user accessor.</param>
		/// <param name="configure">
		/// A delegate to configure the HTTP strategies. Provides helper methods for claim, query string,
		/// and route strategies with customizable parameter names.
		/// </param>
		/// <returns>The given collection of services for chaining calls.</returns>
		public static IServiceCollection AddHttpUserAccessor<TKey>(
			this IServiceCollection services,
			Action<IHttpUserIdentifierStrategyBuilder<TKey>> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			return services.AddUserAccessor<TKey>(builder => {
				var httpBuilder = new HttpUserIdentifierStrategyBuilder<TKey>(builder);
				configure(httpBuilder);
			});
		}
	}

	/// <summary>
	/// Fluent builder interface for configuring HTTP-based user identifier strategies.
	/// </summary>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public interface IHttpUserIdentifierStrategyBuilder<TKey>
	{
		/// <summary>
		/// Adds a claim-based strategy.
		/// </summary>
		/// <param name="claimType">The claim type to read. Defaults to "sub".</param>
		/// <returns>The builder for fluent chaining.</returns>
		IHttpUserIdentifierStrategyBuilder<TKey> AddClaim(string claimType = "sub");

		/// <summary>
		/// Adds a query string-based strategy.
		/// </summary>
		/// <param name="parameter">The query string parameter name. Defaults to "user_id".</param>
		/// <returns>The builder for fluent chaining.</returns>
		IHttpUserIdentifierStrategyBuilder<TKey> AddQueryString(string parameter = "user_id");

		/// <summary>
		/// Adds a route value-based strategy.
		/// </summary>
		/// <param name="key">The route value key. Defaults to "userId".</param>
		/// <returns>The builder for fluent chaining.</returns>
		IHttpUserIdentifierStrategyBuilder<TKey> AddRoute(string key = "userId");

		/// <summary>
		/// Adds a static strategy that returns a fixed user identifier.
		/// </summary>
		/// <param name="userId">The fixed user identifier.</param>
		/// <returns>The builder for fluent chaining.</returns>
		IHttpUserIdentifierStrategyBuilder<TKey> AddStatic(TKey userId);
	}

	internal class HttpUserIdentifierStrategyBuilder<TKey> : IHttpUserIdentifierStrategyBuilder<TKey>
	{
		private readonly IUserIdentifierStrategyBuilder<TKey> inner;

		public HttpUserIdentifierStrategyBuilder(IUserIdentifierStrategyBuilder<TKey> inner)
		{
			this.inner = inner;
		}

		public IHttpUserIdentifierStrategyBuilder<TKey> AddClaim(string claimType = "sub")
		{
			inner.Add(new ClaimUserIdentifierStrategy<TKey>(claimType));
			return this;
		}

		public IHttpUserIdentifierStrategyBuilder<TKey> AddQueryString(string parameter = "user_id")
		{
			inner.Add(new QueryStringUserIdentifierStrategy<TKey>(parameter));
			return this;
		}

		public IHttpUserIdentifierStrategyBuilder<TKey> AddRoute(string key = "userId")
		{
			inner.Add(new RouteUserIdentifierStrategy<TKey>(key));
			return this;
		}

		public IHttpUserIdentifierStrategyBuilder<TKey> AddStatic(TKey userId)
		{
			inner.AddStatic(userId);
			return this;
		}
	}
}
