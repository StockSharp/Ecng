namespace Ecng.Data
{
    using System;

	using Ecng.Common;

	public abstract class RootObject<TDatabase> : NamedObject
		where TDatabase : Database
    {
		protected RootObject(string name, TDatabase database)
			: base(name)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			Database = database;
		}

		public TDatabase Database { get; private set; }

		public abstract void Initialize();
	}
}