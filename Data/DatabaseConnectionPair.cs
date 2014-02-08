namespace Ecng.Data
{
	using System;

	using Ecng.Common;

	public class DatabaseConnectionPair : Equatable<DatabaseConnectionPair>
	{
		public DatabaseProvider Provider { get; private set; }
		public string ConnectionString { get; private set; }

		public DatabaseConnectionPair(DatabaseProvider provider, string connectionString)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

			if (connectionString.IsEmpty())
				throw new ArgumentNullException("connectionString");

			Provider = provider;
			ConnectionString = connectionString;
		}

		public override string ToString()
		{
			return "({0}) {1}".Put(Provider.Name, ConnectionString);
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override DatabaseConnectionPair Clone()
		{
			return new DatabaseConnectionPair(Provider, ConnectionString);
		}

		protected override bool OnEquals(DatabaseConnectionPair other)
		{
			return Provider.GetType() == other.Provider.GetType() && ConnectionString.CompareIgnoreCase(other.ConnectionString);
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return Provider.GetHashCode() ^ ConnectionString.GetHashCode();
		}
	}
}