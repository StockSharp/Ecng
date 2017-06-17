namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	[Serializable]
	public class LazyLoadObject<T> : Wrapper<T>
	{
		private readonly IStorage _storage;

		internal LazyLoadObject(IStorage storage, object id)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			if (id == null)
				throw new ArgumentNullException(nameof(id));

			_storage = storage;
			Id = id;
		}

		public object Id { get; }

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		public override T Value
		{
			get
			{
				if (!HasValue)
					base.Value = _storage.GetById<T>(Id);

				return base.Value;
			}
			set => base.Value = value;
		}

		public override Wrapper<T> Clone()
		{
			throw new NotSupportedException();
		}
	}
}