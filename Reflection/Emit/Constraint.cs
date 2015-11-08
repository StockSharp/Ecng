namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Reflection;

	#endregion

	enum ConstraintTypes
	{
		None,
		Interface,
		Type,
		Attribute,
	}

	public class Constraint
	{
		#region Constraint.ctor()

		public Constraint()
		{
		}

		public Constraint(Type baseType)
		{
			BaseType = baseType;
		}

		public Constraint(GenericParameterAttributes attribute)
		{
			Attribute = attribute;
		}

		#endregion

		#region BaseType

		private Type _baseType;

		public Type BaseType
		{
			get { return _baseType; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_constraintType = value.IsInterface ? ConstraintTypes.Interface : ConstraintTypes.Type;

				_baseType = value;
			}
		}

		#endregion

		#region Attribute

		private GenericParameterAttributes _attribute;

		public GenericParameterAttributes Attribute
		{
			get { return _attribute; }
			set
			{
				_constraintType = ConstraintTypes.Attribute;
				_attribute = value;
			}
		}

		#endregion

		#region ConstraintType

		private ConstraintTypes _constraintType = ConstraintTypes.None;

		internal ConstraintTypes ConstraintType
		{
			get { return _constraintType; }
			set { _constraintType = value; }
		}

		#endregion
	}
}