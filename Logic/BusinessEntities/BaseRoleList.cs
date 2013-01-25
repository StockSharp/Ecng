namespace Ecng.Logic.BusinessEntities
{
	using System;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	[Serializable]
	public abstract class BaseRoleList<TUser, TRole> : BaseEntityList<TRole, TUser, TRole>
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		protected BaseRoleList(IStorage storage)
			: base(storage)
		{
		}

		private readonly static Field _nameField = SchemaManager.GetSchema<TRole>().Fields["Name"];

		public TRole ReadByName([Length(512)]string name)
		{
			return Read(_nameField, name);
		}
	}
}