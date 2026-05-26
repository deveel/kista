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

using Microsoft.EntityFrameworkCore;

using System.Linq.Expressions;
using System.Reflection;

namespace Kista {
	internal static class EfQueryNormalizer {
		public static IQueryable<TEntity> Normalize<TEntity>(IQueryable<TEntity> query)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(query);

			var normalizedExpression = StartsWithToLikeVisitor.Instance.Visit(query.Expression)
				?? throw new InvalidOperationException("The normalized query expression cannot be null.");

			if (ReferenceEquals(normalizedExpression, query.Expression))
				return query;

			return query.Provider.CreateQuery<TEntity>(normalizedExpression);
		}

		public static Expression<Func<TEntity, bool>> Normalize<TEntity>(Expression<Func<TEntity, bool>> predicate)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(predicate);

			var normalizedBody = StartsWithToLikeVisitor.Instance.Visit(predicate.Body)
				?? throw new InvalidOperationException("The normalized predicate body cannot be null.");

			if (ReferenceEquals(normalizedBody, predicate.Body))
				return predicate;

			return Expression.Lambda<Func<TEntity, bool>>(normalizedBody, predicate.Parameters);
		}

		private sealed class StartsWithToLikeVisitor : ExpressionVisitor {
			private static readonly MethodInfo LikeMethod = typeof(DbFunctionsExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(x => x.Name == nameof(DbFunctionsExtensions.Like)
					&& x.GetParameters().Length == 3
					&& x.GetParameters()[0].ParameterType == typeof(DbFunctions)
					&& x.GetParameters()[1].ParameterType == typeof(string)
					&& x.GetParameters()[2].ParameterType == typeof(string));

			private static readonly PropertyInfo FunctionsProperty = typeof(EF)
				.GetProperty(nameof(EF.Functions), BindingFlags.Public | BindingFlags.Static)
				?? throw new InvalidOperationException("Cannot resolve EF.Functions property.");

			private static readonly MethodInfo StringConcatMethod = typeof(string)
				.GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })
				?? throw new InvalidOperationException("Cannot resolve string.Concat(string, string).");

			/// <summary>
			/// Gets the singleton instance of the visitor.
			/// </summary>
			public static StartsWithToLikeVisitor Instance { get; } = new StartsWithToLikeVisitor();

			protected override Expression VisitMethodCall(MethodCallExpression node) {
				if (node.Method.Name == nameof(string.StartsWith)
					&& node.Object?.Type == typeof(string)
					&& node.Arguments.Count == 1
					&& node.Arguments[0].Type == typeof(string)) {
					var instance = Visit(node.Object)
						?? throw new InvalidOperationException("StartsWith instance cannot be null.");
					var value = Visit(node.Arguments[0])
						?? throw new InvalidOperationException("StartsWith argument cannot be null.");

					var pattern = Expression.Call(StringConcatMethod, value, Expression.Constant("%"));
					var functions = Expression.Property(null, FunctionsProperty);
					var like = Expression.Call(LikeMethod, functions, instance, pattern);

					var instanceNotNull = Expression.NotEqual(instance, Expression.Constant(null, typeof(string)));
					var valueNotNull = Expression.NotEqual(value, Expression.Constant(null, typeof(string)));

					return Expression.AndAlso(instanceNotNull, Expression.AndAlso(valueNotNull, like));
				}

				return base.VisitMethodCall(node);
			}
		}
	}
}

