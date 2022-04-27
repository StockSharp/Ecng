namespace Ecng.Serialization
{
	using System;

	public class SerializationContext
	{
		public object Entity { get; }

		public SerializationContext(object entity)
		{
			Entity = entity ?? throw new ArgumentNullException(nameof(entity));
		}
	}
}