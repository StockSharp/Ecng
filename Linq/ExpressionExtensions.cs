namespace Ecng.Linq;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Ecng.Common;

/// <summary>
/// The extensions for <see cref="Expression"/>.
/// </summary>
public static class ExpressionExtensions
{
	/// <summary>
	/// Gets the constant value from the expression.
	/// </summary>
	/// <typeparam name="T">The type of the constant value.</typeparam>
	/// <param name="e">The expression.</param>
	/// <returns>The constant value.</returns>
	public static T GetConstant<T>(this Expression e)
		=> ((ConstantExpression)e).Value.To<T>();

	/// <summary>
	/// Evaluates the expression and returns its value.
	/// </summary>
	/// <param name="e">The expression to evaluate.</param>
	/// <returns>The evaluated value.</returns>
	public static object Evaluate(this Expression e)
	{
		//A little optimization for constant expressions
		if (e.NodeType == ExpressionType.Constant)
			return e.GetConstant<object>();

		return Expression.Lambda(e).Compile().DynamicInvoke();
	}

	/// <summary>
	/// Replaces the source provider in the expression with the specified query provider.
	/// </summary>
	/// <param name="expression">The expression to modify.</param>
	/// <param name="provider">The query provider to set.</param>
	/// <exception cref="ArgumentNullException">Thrown when expression or provider is null.</exception>
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

	/// <summary>
	/// Removes quote expressions from the given expression.
	/// </summary>
	/// <param name="e">The expression to process.</param>
	/// <returns>The unquoted expression.</returns>
	public static Expression StripQuotes(this Expression e)
	{
		while (e.NodeType == ExpressionType.Quote)
		{
			e = ((UnaryExpression)e).Operand;
		}

		return e;
	}

	/// <summary>
	/// Gets the value of the specified member from the given instance.
	/// </summary>
	/// <param name="member">The member whose value to retrieve.</param>
	/// <param name="instance">The object instance from which to retrieve the value.</param>
	/// <returns>The value of the member.</returns>
	/// <exception cref="NotSupportedException">Thrown when the member type is not supported.</exception>
	public static object GetMemberValue(this MemberInfo member, object instance)
	{
		if (member is PropertyInfo pi)
			return pi.GetValue(instance);
		else if (member is FieldInfo fi)
			return fi.GetValue(instance);
		else
			throw new NotSupportedException();
	}

	/// <summary>
	/// Retrieves the innermost member in a nested member expression.
	/// </summary>
	/// <param name="exp">The member expression to process.</param>
	/// <returns>The innermost member expression.</returns>
	public static MemberExpression GetInnerMember(this MemberExpression exp)
	{
		if (exp.Expression is MemberExpression d)
			return GetInnerMember(d);

		return exp;
	}

	/// <summary>
	/// Evaluates the expression and returns its value as the specified type.
	/// </summary>
	/// <typeparam name="TValue">The type to convert the evaluated value to.</typeparam>
	/// <param name="exp">The expression to evaluate.</param>
	/// <returns>The evaluated value converted to the specified type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the expression is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the expression type is not supported.</exception>
	public static TValue GetValue<TValue>(this Expression exp)
	{
		if (exp is null)
			throw new ArgumentNullException(nameof(exp));

		if (exp is ConstantExpression c)
			return c.Value.To<TValue>();
		else if (exp is MemberExpression me)
			return me.Member.GetMemberValue(me.Expression is null ? null : GetValue<object>(me.Expression)).To<TValue>();

		throw new ArgumentOutOfRangeException(exp.NodeType.ToString());
	}

	/// <summary>
	/// Converts the given expression type to its equivalent <see cref="ComparisonOperator"/>.
	/// </summary>
	/// <param name="type">The expression type.</param>
	/// <returns>The corresponding <see cref="ComparisonOperator"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the expression type has no corresponding operator.</exception>
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