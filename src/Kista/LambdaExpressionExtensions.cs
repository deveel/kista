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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Kista {
	static class LambdaExpressionExtensions {
		public static Expression<Func<TTarget, bool>> As<TTarget>(this LambdaExpression expression) {
			if (expression.ReturnType != typeof(bool))
				throw new ArgumentException();
			if (expression.Parameters.Count != 1)
				throw new ArgumentException();

			var param = expression.Parameters[0];

			if (param.Type == typeof(TTarget))
				return (Expression<Func<TTarget, bool>>)expression;

			if (!param.Type.IsAssignableFrom(typeof(TTarget)))
				throw new ArgumentException("The expression parameter is not assignable from the target type");

			var parameter = Expression.Parameter(typeof(TTarget), param.Name);
			var body = ReplaceType(expression.Body, param.Type, typeof(TTarget));

			return Expression.Lambda<Func<TTarget, bool>>(body, false, parameter);
		}

		private static Expression ReplaceType(Expression expression, Type sourceType, Type destType) {
			var replacer = new TypeReplacer(sourceType, destType);
			return replacer.Visit(expression);
		}

		class TypeReplacer : ExpressionVisitor {
			private readonly Type sourceType;
			private readonly Type destType;

			public TypeReplacer(Type sourceType, Type destType) {
				this.sourceType = sourceType;
				this.destType = destType;
			}

			protected override Expression VisitParameter(ParameterExpression node) {
				if (node.Type == sourceType) {
					node = Expression.Parameter(destType, node.Name);
				}

				return base.VisitParameter(node);
			}

			protected override Expression VisitMember(MemberExpression node) {
				if (node.Member.ReflectedType == sourceType) {
					var newMember = destType.GetMember(node.Member.Name, BindingFlags.Public | BindingFlags.Instance);
					if (newMember.Length > 1)
						throw new AmbiguousMatchException();

					node = Expression.MakeMemberAccess(Visit(node.Expression), newMember[0]);
				}

				return base.VisitMember(node);
			}
		}
	}
}