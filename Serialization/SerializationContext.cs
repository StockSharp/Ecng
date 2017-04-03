namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;

	public class SerializationContext
	{
		public Func<IEnumerable<Field>, IEnumerable<Field>> Filter { get; set; }

		public object Entity { get; set; }

		public static StorageDelayAction DelayAction { get; set; }
	}
}