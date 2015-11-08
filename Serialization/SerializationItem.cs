namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;

	#endregion

	public class SerializationItem : Equatable<SerializationItem>
	{
		#region SerializationItem.ctor()

		public SerializationItem(Field field, object value)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));

			Field = field;
			Value = value;
		}

		#endregion

		public Field Field { get; }

		#region Value

		private object _value;

		public object Value
		{
			get { return _value; }
			set
			{
				//if (value != null)
				//{
				//    if (Field.Type != value.GetType())
				//    {
				//        var valueType = Field.Factory != null ? Field.Factory.SourceType : Field.Type;
				//        if (!(value is SerializationItemCollection) && !valueType.IsInstanceOfType(value))
				//            throw new ArgumentException("Value '{0}' isn't compatible with field '{1}' of type '{2}'.".Put(value, Field.Name, valueType), "value");
				//    }
				//}

				_value = value;
			}
		}

		#endregion

		#region Object Members

		public override string ToString()
		{
			return "Field = '{0}' Value = '{1}'".Put(Field, Value);
		}

		#endregion

		#region Overrides of Cloneable<SerializationItem>

		public override SerializationItem Clone()
		{
			var clone = new SerializationItem(Field, Value);

			if (Value != null)
			{
				if (Value is SerializationItemCollection)
					clone.Value = ((SerializationItemCollection)Value).Clone();
				else
					clone.Value = Value;
			}

			return clone;
		}

		#endregion

		#region Overrides of Equatable<SerializationItem>

		protected override bool OnEquals(SerializationItem other)
		{
			return Field == other.Field && object.Equals(Value, other.Value);
		}

		#endregion
	}

	public class SerializationItem<T> : SerializationItem
	{
		#region SerializationItem.ctor()

		public SerializationItem(Field field, T value)
			: base(field, value)
		{
		}

		#endregion

		#region Value

		public new T Value
		{
			get { return (T)base.Value; }
			set { base.Value = value; }
		}

		#endregion
	}
}