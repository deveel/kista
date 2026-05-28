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
