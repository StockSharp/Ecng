namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	[Serializable]
	public abstract class BaseRoleList<TUser, TRole> : BaseEntityList<TRole, TUser, TRole>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		protected BaseRoleList(IStorage storage)
			: base(storage)
		{
		}

		private static readonly Field _nameField = SchemaManager.GetSchema<TRole>().Fields["Name"];

		public TRole ReadByName([Length(512)]string name)
		{
			return Read(_nameField, name);
		}
	}
}