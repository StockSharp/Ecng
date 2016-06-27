namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;

	[Serializable]
	public abstract class BaseUserList<TUser, TRole> : BaseEntityList<TUser, TUser, TRole>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		private readonly long _nullUserId;

		protected BaseUserList(IStorage storage, long nullUserId)
			: base(storage)
		{
			_nullUserId = nullUserId;
		}

		private static TUser _null;

		public TUser Null => _null ?? (_null = ReadById(_nullUserId));

		public abstract TUser ReadByName(string userName);
	}
}