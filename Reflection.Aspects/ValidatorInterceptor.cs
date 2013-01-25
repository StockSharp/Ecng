namespace Ecng.Reflection.Aspects
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	public class ValidatorInterceptor : Interceptor
	{
		#region Private Fields

		private static readonly string _returnValidatorId = Guid.NewGuid().ToString();

		private static readonly Dictionary<Tuple<Type, MethodBase>, Dictionary<string, BaseValidator>> _validators = new Dictionary<Tuple<Type, MethodBase>, Dictionary<string, BaseValidator>>();

		#endregion

		#region Interceptor Members

		protected internal override void BeforeCall(InterceptContext context)
		{
			var key = new Tuple<Type, MethodBase>(context.ReflectedType, context.Method);

			var validators = _validators.SafeAdd(key, delegate
			{
				var dict = new Dictionary<string, BaseValidator>();

				foreach (var param in context.Method.GetParameters())
				{
					var attr = param.GetAttribute<BaseValidatorAttribute>();
					if (attr != null)
						dict.Add(param.Name, attr.CreateValidator(param.ParameterType));
				}

				if (context.Method is MethodInfo)
				{
					var owner = ((MethodInfo)context.Method).GetAccessorOwner();
					if (owner != null)
					{
						var attr = owner.GetAttribute<BaseValidatorAttribute>();
						if (attr != null)
						{
							var memberType = owner.GetMemberType();

							if (owner is EventInfo)
								dict.Add("value", attr.CreateValidator(memberType));
							else if (owner is PropertyInfo)
								dict.Add(context.HasReturnValue ? _returnValidatorId : "value", attr.CreateValidator(memberType));
							else
								throw new ArgumentException("context");
						}
					}
					else
					{
						if (context.HasReturnValue)
						{
							var returnParam = ((MethodInfo)context.Method).ReturnParameter;

							var attr = returnParam.GetAttribute<BaseValidatorAttribute>();
							if (attr != null)
								dict.Add(_returnValidatorId, attr.CreateValidator(returnParam.ParameterType));
						}
					}
				}

				return dict;
			});

			Validate(validators, context.InRefArgs);
			
			base.BeforeCall(context);
		}

		protected internal override void AfterCall(InterceptContext context)
		{
			var key = new Tuple<Type, MethodBase>(context.ReflectedType, context.Method);

			var validators = _validators[key];

			Validate(validators, context.RefOutArgs);

			if (context.HasReturnValue)
			{
				BaseValidator validator;

				if (validators.TryGetValue(_returnValidatorId, out validator))
					validator.Validate(context.ReturnValue);
			}

			base.AfterCall(context);
		}

		#endregion

		private static void Validate(Dictionary<string, BaseValidator> validators, IEnumerable<KeyValuePair<string, object>> args)
		{
			foreach (var pair in args)
			{
				BaseValidator validator;

				if (validators.TryGetValue(pair.Key, out validator))
					validator.Validate(pair.Value);
			}
		}
	}
}