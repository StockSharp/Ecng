namespace Ecng.Reflection.Emit
{
	using System;
	using System.Reflection;
	using System.Reflection.Emit;

	using Ecng.Common;
	using Ecng.Collections;

	public class ConstraintList : BaseList<Constraint>
	{
		private readonly GenericTypeParameterBuilder _builder;

		internal ConstraintList(GenericTypeParameterBuilder builder)
		{
			_builder = builder;
		}

		public void Add(GenericParameterAttributes attribute)
		{
			base.Add(new Constraint(attribute));
		}

		public void Add(Type baseType)
		{
			base.Add(new Constraint(baseType));
		}

		protected override void OnAdded(Constraint item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			switch (item.ConstraintType)
			{
				case ConstraintTypes.None:
					throw new ArgumentException(nameof(item));
				case ConstraintTypes.Interface:
					_builder.SetInterfaceConstraints(item.BaseType);
					break;
				case ConstraintTypes.Type:
					_builder.SetBaseTypeConstraint(item.BaseType);
					break;
				case ConstraintTypes.Attribute:
					_builder.SetGenericParameterAttributes(item.Attribute);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(item), item.ConstraintType.To<string>());
			}

			base.OnAdded(item);
		}
	}
}