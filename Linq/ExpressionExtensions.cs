namespace Ecng.Linq
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

		public static Expression StripQuotes(this Expression e)
		{
			while (e.NodeType == ExpressionType.Quote)
			{
				e = ((UnaryExpression)e).Operand;
			}

			return e;
		}

		public static object GetMemberValue(this MemberInfo member, object instance)
		{
			if (member is PropertyInfo pi)
				return pi.GetValue(instance);
			else if (member is FieldInfo fi)
				return fi.GetValue(instance);
			else
				throw new NotSupportedException();
		}

		public static MemberExpression GetInnerMember(this MemberExpression exp)
		{
			if (exp.Expression is MemberExpression d)
				return GetInnerMember(d);

			return exp;
		}

		public static TValue GetValue<TValue>(this Expression exp)
		{
			if (exp is ConstantExpression c)
				return c.Value.To<TValue>();
			else if (exp is MemberExpression me)
				return me.Member.GetMemberValue(GetValue<object>(me.Expression)).To<TValue>();

			throw new ArgumentOutOfRangeException(exp.NodeType.ToString());
		}

		public static ComparisonOperator ToOperator(this ExpressionType type)
		{
			return type switch
			{
				ExpressionType.GreaterThan => ComparisonOperator.Greater,
				ExpressionType.GreaterThanOrEqual => ComparisonOperator.GreaterOrEqual,
				ExpressionType.LessThan => ComparisonOperator.Less,
				ExpressionType.LessThanOrEqual => ComparisonOperator.LessOrEqual,
				ExpressionType.Equal => ComparisonOperator.Equal,
				ExpressionType.NotEqual => ComparisonOperator.NotEqual,
				_ => throw new ArgumentOutOfRangeException(type.To<string>()),
			};
		}
	}
}