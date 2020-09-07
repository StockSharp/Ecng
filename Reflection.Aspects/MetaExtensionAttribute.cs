namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;

	#endregion

	[AttributeUsage(ReflectionHelper.Types, AllowMultiple = true)]
	public class MetaExtensionAttribute : OrderedAttribute
	{
		protected internal virtual void Extend(MetaExtensionContext context, int order)
		{
			ApplyTypeAttributes(context);

			if (context.BaseType.IsGenericTypeDefinition)
			{
				var args = context.BaseType.GetGenericArgs().ToArray();

				var names = args.Select(arg => arg.Name).ToArray();

				int index = 0;
				foreach (var argGenerator in context.TypeGenerator.CreateGenericParameters(names))
				{
					argGenerator.Constraints.AddRange(args[index].Constraints);
					argGenerator.Attributes.AddRange(AttributeGenerator.CreateAttrs(args[index].Type));
					index++;
				}
			}

			foreach (var method in GetImplementingMethods<MethodInfo>(context))
			{
				var attr = GetExtensionAttribute(method, order);
				if (attr != null)
				{
					var methodGen = context.TypeGenerator.CreateMethod(method.Name, GetAttributes(context.BaseType, method), method.ReturnType, method.GetParameterTypes());

					if (method.IsGenericMethodDefinition)
					{
						var args = method.GetGenericArgs().ToArray();

						var names = args.Select(arg => arg.Name).ToArray();

						int index = 0;
						foreach (var argGenerator in methodGen.CreateGenericParameters(names))
						{
							argGenerator.Constraints.AddRange(args[index].Constraints);
							argGenerator.Attributes.AddRange(AttributeGenerator.CreateAttrs(args[index].Type));
							index++;
						}
					}

					attr.Implement(context, methodGen, method);
				}
			}

			foreach (var ctor in GetImplementingMethods<ConstructorInfo>(context))
			{
				var attr = GetExtensionAttribute(ctor, order);
				if (attr != null)
				{
					var ctorGen = context.TypeGenerator.CreateConstructor(MethodAttributes.Public, ctor.GetParameterTypes());
					attr.Implement(context, ctorGen, ctor);
				}
			}
		}

		protected virtual void ApplyTypeAttributes(MetaExtensionContext context)
		{
			foreach (var attrData in CustomAttributeData.GetCustomAttributes(context.BaseType))
			{
				if (!attrData.Constructor.ReflectedType.IsAssignableFrom(typeof(MetaExtensionAttribute)))
					context.TypeGenerator.Attributes.Add(new AttributeGenerator(attrData));
			}
		}

		protected virtual MetaExtensionMethodAttribute GetExtensionAttribute(MethodBase method, int order)
		{
			var attr = FindAttribute(method, order);

			if (attr != null)
				return attr;
			else
			{
				if (method is MethodInfo mi)
				{
					var owner = mi.GetAccessorOwner();

					if (owner != null)
					{
						attr = FindAttribute(owner, order);

						if (attr != null)
							return attr;
					}
				}

				if (method.IsAbstract || method is ConstructorInfo)
					return new DefaultImpAttribute();
			}

			return null;
		}

		protected virtual IEnumerable<T> GetImplementingMethods<T>(MetaExtensionContext context)
			where T : MethodBase
		{
			return context.BaseType.GetMembers<T>(ReflectionHelper.AllInstanceMembers).Where(ReflectionHelper.IsOverloadable);
		}

		private static MetaExtensionMethodAttribute FindAttribute(MemberInfo member, int order)
		{
			return member.GetAttributes<MetaExtensionMethodAttribute>().FirstOrDefault(arg => arg.Order == order);
		}

		private static MethodAttributes GetAttributes(Type baseType, MethodBase method)
		{
			if (baseType.IsClass)
				return method.Attributes.Remove(MethodAttributes.Abstract | MethodAttributes.NewSlot);
			else if (baseType.IsInterface)
				return method.Attributes.Remove(MethodAttributes.Abstract);// | MethodAttributes.Final | MethodAttributes.Private;
			else
				throw new ArgumentException("baseType");
		}
	}
}