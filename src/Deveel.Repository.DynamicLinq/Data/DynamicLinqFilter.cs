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

using System.Linq.Expressions;

namespace Deveel.Data {
	/// <summary>
	/// Represents a query filter defined by a Dynamic LINQ expression string
	/// that can be applied to an <see cref="IQueryable{T}"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="DynamicLinqFilter"/> implements <see cref="IExpressionQueryFilter"/>
	/// and serves as a bridge between string-based filter expressions (commonly received
	/// from API clients or configuration) and the strongly-typed expression tree API
	/// used by LINQ providers.
	/// </para>
	/// <para>
	/// The expression string uses the syntax supported by <c>System.Linq.Dynamic.Core</c>.
	/// For example: <c>"x.FirstName == \"John\" &amp;&amp; x.Age > 18"</c>.
	/// </para>
	/// <para>
	/// When an <see cref="IExpressionCache"/> is provided via the constructor, the
	/// <see cref="AsLambda{TEntity}"/> method will cache parsed expressions to avoid
	/// re-parsing the same expression string on subsequent calls. This is particularly
	/// beneficial in multi-tenant scenarios where the same filter shape is applied
	/// repeatedly across different requests.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Without caching
	/// var filter = new DynamicLinqFilter("x.Status == \"Active\"");
	/// 
	/// // With caching for improved performance
	/// var cache = new BoundedExpressionCache(2048);
	/// var filter = new DynamicLinqFilter("x.Status == \"Active\"", cache);
	/// 
	/// // Apply to a repository query
	/// var results = await repository.FindAllAsync(filter);
	/// </code>
	/// </example>
	/// <seealso cref="IExpressionCache"/>
	/// <seealso cref="FilterExpression"/>
	/// <seealso cref="IExpressionQueryFilter"/>
	public sealed class DynamicLinqFilter : IExpressionQueryFilter {
		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicLinqFilter"/> class
		/// with the specified parameter name, expression string, and optional cache.
		/// </summary>
		/// <param name="paramName">
		/// The name of the parameter to be used in the expression (e.g., <c>"x"</c> or <c>"p"</c>).
		/// </param>
		/// <param name="expression">
		/// The Dynamic LINQ expression string to be used as a filter.
		/// </param>
		/// <param name="cache">
		/// An optional <see cref="IExpressionCache"/> used to store and retrieve parsed
		/// expressions. When provided, <see cref="AsLambda{TEntity}"/> will cache the
		/// parsed expression to avoid re-parsing on subsequent calls.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either <paramref name="paramName"/> or <paramref name="expression"/> is <c>null</c>.
		/// </exception>
		public DynamicLinqFilter(string paramName, string expression, IExpressionCache? cache = null) {
			ArgumentNullException.ThrowIfNull(paramName, nameof(paramName));
			ArgumentNullException.ThrowIfNull(expression, nameof(expression));

			ParameterName = paramName;
			Expression = expression;
			Cache = cache;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicLinqFilter"/> class
		/// with the specified expression string and optional cache, using the
		/// <see cref="DefaultParameterName"/> as the parameter name.
		/// </summary>
		/// <param name="expression">
		/// The Dynamic LINQ expression string to be used as a filter.
		/// </param>
		/// <param name="cache">
		/// An optional <see cref="IExpressionCache"/> used to store and retrieve parsed
		/// expressions. When provided, <see cref="AsLambda{TEntity}"/> will cache the
		/// parsed expression to avoid re-parsing on subsequent calls.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="expression"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// This constructor uses <see cref="DefaultParameterName"/> (<c>"x"</c>) as the
		/// parameter name in the expression. Use the
		/// <see cref="DynamicLinqFilter(string, string, IExpressionCache?)"/> constructor
		/// to specify a custom parameter name.
		/// </remarks>
		public DynamicLinqFilter(string expression, IExpressionCache? cache = null)
			: this(DefaultParameterName, expression, cache) {
		}

		/// <summary>
		/// The default name of the parameter to be used
		/// in a filter expression.
		/// </summary>
		public const string DefaultParameterName = "x";

		/// <summary>
		/// Gets the Dynamic LINQ expression string that defines the filter condition.
		/// </summary>
		/// <value>
		/// A string containing a valid Dynamic LINQ expression, such as
		/// <c>"x.FirstName == \"John\""</c>.
		/// </value>
		public string Expression { get; }

		/// <summary>
		/// Gets the name of the parameter used in the expression string.
		/// </summary>
		/// <value>
		/// A string representing the parameter name (e.g., <c>"x"</c> or <c>"p"</c>).
		/// </value>
		public string ParameterName { get; }

	/// <summary>
	/// Gets the optional <see cref="IExpressionCache"/> used to cache parsed
	/// expressions and avoid re-parsing on repeated calls to <see cref="AsLambda{TEntity}"/>.
	/// </summary>
	/// <value>
	/// An <see cref="IExpressionCache"/> instance if one was provided during construction
	/// or resolved via <see cref="Initialize"/>; otherwise, <c>null</c>.
	/// </value>
	public IExpressionCache? Cache { get; private set; }

	/// <inheritdoc />
	/// <remarks>
	/// <para>
	/// When the repository applies this filter to a query, it calls <see cref="Initialize"/>
	/// to give the filter access to the repository's service provider. This method resolves
	/// an <see cref="IExpressionCache"/> from the context if one is registered and no cache
	/// was provided via the constructor.
	/// </para>
	/// <para>
	/// A cache provided through the constructor takes precedence over one resolved from
	/// the context. This allows callers to override the default cache for specific queries.
	/// </para>
	/// </remarks>
	public void Initialize(IFilterContext context) {
		if (Cache != null)
			return;

		Cache = context.Services.GetService(typeof(IExpressionCache)) as IExpressionCache;
	}

	/// <summary>
		/// Converts this filter into a typed <see cref="Expression{TDelegate}"/>
		/// (<c>Func{TEntity, bool}</c>) that can be applied to an <see cref="IQueryable{TEntity}"/>.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The entity type of the queryable to filter. Must be a reference type.
		/// </typeparam>
		/// <returns>
		/// An expression tree representing the filter predicate for the specified entity type.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the expression string cannot be parsed or does not produce a boolean result.
		/// </exception>
		/// <remarks>
		/// <para>
		/// If a <see cref="Cache"/> was provided during construction, the parsed expression
		/// is cached and reused on subsequent calls with the same entity type, parameter name,
		/// and expression string.
		/// </para>
		/// <para>
		/// The expression string is parsed using <see cref="FilterExpression.AsLambda{T}(IExpressionCache?, string, string)"/>.
		/// </para>
		/// </remarks>
		/// <seealso cref="FilterExpression.AsLambda{T}(IExpressionCache?, string, string)"/>
		public Expression<Func<TEntity, bool>> AsLambda<TEntity>() where TEntity : class {
			return FilterExpression.AsLambda<TEntity>(Cache, ParameterName, Expression);
		}
	}
}
