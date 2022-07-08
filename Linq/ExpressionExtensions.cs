namespace Ecng.Async
{
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;

	using Ecng.Common;

	public static class ExpressionExtensions
	{
		public static T GetConstant<T>(this Expression e)
			=> ((ConstantExpression)e).Value.To<T>();

		public static object Evaluate(this Expression e)
		{
			//A little optimization for constant expressions
			if (e.NodeType == ExpressionType.Constant)
				return e.GetConstant<object>();

			return Expression.Lambda(e).Compile().DynamicInvoke();
		}

		public static void ReplaceSource(this Expression expression, IQueryProvider provider)
		{
			if (expression is null)
				throw new ArgumentNullException(nameof(expression));

			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			while (expression is MethodCallExpression mce)
				expression = mce.Arguments[0];

			var field = expression.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First();
			field.SetValue(expression, provider);
		}
	}
}