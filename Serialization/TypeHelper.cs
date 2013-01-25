namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	#endregion

	public static class TypeHelper
	{
		#region IsPrimitiveType

		public static bool IsPrimitiveType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return	(
						type.IsPrimitive ||
						type.IsEnum ||
						type == typeof(decimal) ||
						type == typeof(string) ||
						type == typeof(DateTime) ||
						type == typeof(Guid) ||
						type == typeof(byte[]) ||
						type == typeof(TimeSpan)
					);
		}

		#endregion
	}
}