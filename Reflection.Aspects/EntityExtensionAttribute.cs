namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using Ecng.Common;
	using Ecng.Reflection.Emit;
	using Ecng.Serialization;

	#endregion

	public class EntityExtensionAttribute : MetaExtensionAttribute
	{
		#region MetaExtensionAttribute Members

		protected internal override void Extend(MetaExtensionContext context, int order)
		{
			if (context.BaseType.GetAttribute<EntityAttribute>() == null)
				context.TypeGenerator.Attributes.Add(new AttributeGenerator(typeof(EntityAttribute), context.BaseType.Name));

			base.Extend(context, order);

			foreach (var field in context.TypeGenerator.Fields)
			{
				if (!field.Builder.IsStatic)
				{
					if (!field.Attributes.Exists(arg =>
						arg.Ctor.DeclaringType.IsAssignableFrom(typeof(FieldAttribute))))
					{
						field.Attributes.Add(new AttributeGenerator(typeof(FieldAttribute), field.Builder.Name.Remove(0, 1)));
					}
				}
			}
		}

		#endregion
	}
}