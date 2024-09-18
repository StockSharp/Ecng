//
// Copyright (C) axuno gGmbH, Scott Rippey, Bernhard Millauer and other contributors.
// Licensed under the MIT license.
//

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SmartFormat.Core.Extensions;

namespace SmartFormat.Extensions
{
    public class ReflectionSource : IAsyncSource
    {
        public ReflectionSource(SmartFormatter formatter)
        {
            // Add some special info to the parser:
            formatter.Parser.AddAlphanumericSelectors(); // (A-Z + a-z)
            formatter.Parser.AddAdditionalSelectorChars("_");
            formatter.Parser.AddOperators(".");
        }

		async ValueTask<bool> IAsyncSource.TryEvaluateSelectorAsync(ISelectorInfo selectorInfo, CancellationToken cancellationToken)
		{
			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

			var current = selectorInfo.CurrentValue;
			var selector = selectorInfo.SelectorText;

			if (current == null) return false;

			// REFLECTION:
			// Let's see if the argSelector is a Selectors/Field/ParseFormat:
			var sourceType = current.GetType();

			// Important:
			// GetMembers (opposite to GetMember!) returns all members, 
			// both those defined by the type represented by the current T:System.Type object 
			// AS WELL AS those inherited from its base types.
			var members = sourceType.GetMembers(bindingFlags).Where(m =>
				string.Equals(m.Name, selector, selectorInfo.FormatDetails.Settings.GetCaseSensitivityComparison()));
			foreach (var member in members)
				switch (member.MemberType)
				{
					case MemberTypes.Field:
						//  Selector is a Field; retrieve the value:
						var field = (FieldInfo)member;
						selectorInfo.Result = field.GetValue(current);
						return true;
					case MemberTypes.Property:
					case MemberTypes.Method:
						MethodInfo? method;
						if (member.MemberType == MemberTypes.Property)
						{
							//  Selector is a Property
							var prop = (PropertyInfo)member;
							//  Make sure the property is not WriteOnly:
							if (prop != null && prop.CanRead)
								method = prop.GetGetMethod();
							else
								continue;
						}
						else
						{
							//  Selector is a method
							method = (MethodInfo)member;
						}

						if (method is not null)
						{
							if (method.GetParameters().Length > 1) continue;

							var retType = method.ReturnType;

							if (retType == typeof(void) || retType == typeof(ValueTask) || retType == typeof(Task)) continue;

							if (retType.IsGenericType
								&&
								(
									retType.GetGenericTypeDefinition() == typeof(ValueTask<>) ||
									retType.GetGenericTypeDefinition() == typeof(Task<>)
								))
							{
								var result = method.Invoke(current, new object[] { cancellationToken });

								Task? task;

								if (retType.GetGenericTypeDefinition() == typeof(ValueTask<>))
								{
									if (result is null)
										throw new InvalidOperationException("result is null.");

									task = (Task?)result.GetType().GetMethod(nameof(ValueTask<object>.AsTask))!.Invoke(result, []);
								}
								else
									task = (Task?)result;

								if (task is null)
									throw new InvalidOperationException("task is null.");

								await task;
								selectorInfo.Result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
							}
							else
								selectorInfo.Result = method.Invoke(current, []);
						}
						
						return true;
				}

			return false;
		}

		public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

            var current = selectorInfo.CurrentValue;
            var selector = selectorInfo.SelectorText;

            if (current == null) return false;

            // REFLECTION:
            // Let's see if the argSelector is a Selectors/Field/ParseFormat:
            var sourceType = current.GetType();

            // Important:
            // GetMembers (opposite to GetMember!) returns all members, 
            // both those defined by the type represented by the current T:System.Type object 
            // AS WELL AS those inherited from its base types.
            var members = sourceType.GetMembers(bindingFlags).Where(m =>
                string.Equals(m.Name, selector, selectorInfo.FormatDetails.Settings.GetCaseSensitivityComparison()));
            foreach (var member in members)
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        //  Selector is a Field; retrieve the value:
                        var field = (FieldInfo) member;
                        selectorInfo.Result = field.GetValue(current);
                        return true;
                    case MemberTypes.Property:
                    case MemberTypes.Method:
                        MethodInfo? method;
                        if (member.MemberType == MemberTypes.Property)
                        {
                            //  Selector is a Property
                            var prop = (PropertyInfo) member;
                            //  Make sure the property is not WriteOnly:
                            if (prop != null && prop.CanRead)
                                method = prop.GetGetMethod();
                            else
                                continue;
                        }
                        else
                        {
                            //  Selector is a method
                            method = (MethodInfo) member;
                        }

                        //  Check that this method is valid -- it needs to return a value and has to be parameter-less:
                        //  We are only looking for a parameter-less Function/Property:
                        if (method?.GetParameters().Length > 0) continue;

                        //  Make sure that this method is not void!  It has to be a Function!
                        if (method?.ReturnType == typeof(void)) continue;

                        //  Retrieve the Selectors/ParseFormat value:
                        selectorInfo.Result = method?.Invoke(current, new object[0]);
                        return true;
                }

            return false;
        }
    }
}