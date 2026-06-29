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

using System.Linq.Expressions;

namespace Kista
{
	internal class ParameterReplacer : ExpressionVisitor
	{
		private readonly ParameterExpression _source;
		private readonly ParameterExpression _target;

		public ParameterReplacer(ParameterExpression source, ParameterExpression target)
		{
			_source = source;
			_target = target;
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			return node == _source ? _target : base.VisitParameter(node);
		}

		public static Expression<Func<T, bool>> Combine<T>(
			Expression<Func<T, bool>> left,
			Expression<Func<T, bool>> right)
		{
			var replacer = new ParameterReplacer(right.Parameters[0], left.Parameters[0]);
			var rightBody = replacer.Visit(right.Body);
			var combinedBody = Expression.AndAlso(left.Body, rightBody);
			return Expression.Lambda<Func<T, bool>>(combinedBody, left.Parameters);
		}
	}
}
