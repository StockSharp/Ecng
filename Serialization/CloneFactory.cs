namespace Ecng.Serialization
{
	#region Using Directives

	using System;

    using Ecng.Common;

	#endregion

	public abstract class CloneFactory<T>
	{
		#region CloneFactory.ctor()

		static CloneFactory()
		{
			if (typeof(T).IsPrimitive() && typeof(T) != typeof(byte[]))
				_factory = new PrimitiveCloneFactory<T>();
			else if (typeof(T).IsArray && typeof(T).GetElementType().IsPrimitive)
				_factory = new ArrayCloneFactory<T>();
			else if (typeof(T).IsAssignableFrom(typeof(Cloneable<T>)) || typeof(T).IsAssignableFrom(typeof(ICloneable)))
				_factory = new SimpleCloneFactory<T>();
			else
				_factory = new BinarySerializerCloneFactory<T>();
		}

		#endregion

		#region Factory

		private static readonly CloneFactory<T> _factory;

		public static CloneFactory<T> Factory
		{
			get { return _factory; }
		}

		#endregion

		public abstract T Clone(T value);
	}

    class SimpleCloneFactory<T> : CloneFactory<T>
	{
		#region CloneFactory<T> Members

		public override T Clone(T value)
        {
			var cloneable = value as Cloneable<T>;

			if (cloneable != null)
				return cloneable.Clone();
            else
                return (T)((ICloneable)value).Clone();
        }

        #endregion
    }

    class BinarySerializerCloneFactory<T> : CloneFactory<T>
	{
		#region Private Fields

		private static readonly BinarySerializer<T> _serializer = new BinarySerializer<T>();

		#endregion

		#region CloneFactory<T> Members

		public override T Clone(T value)
        {
			return _serializer.Deserialize(_serializer.Serialize(value));
        }

        #endregion
    }

    class PrimitiveCloneFactory<T> : CloneFactory<T>
	{
		#region CloneFactory<T> Members

		public override T Clone(T value)
        {
            return value;
        }

        #endregion
    }

    class ArrayCloneFactory<T> : CloneFactory<T>
	{
		#region CloneFactory<T> Members

		public override T Clone(T value)
        {
            var clone = Activator.CreateInstance<T>();
			value.To<Array>().CopyTo(clone.To<Array>(), 0);
            return clone;
        }

        #endregion
    }
}