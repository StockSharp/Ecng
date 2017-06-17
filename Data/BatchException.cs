namespace Ecng.Data
{
	using System;

	public class BatchException<E> : Exception
	{
		public BatchException(E entity, Exception innerException)
			: base("Batch command thrown a exception.", innerException)
		{
			Entity = entity;
		}

		public E Entity { get; }
	}
}