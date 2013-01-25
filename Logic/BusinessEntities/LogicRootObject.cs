namespace Ecng.Logic.BusinessEntities
{
	using Ecng.Data;

	public abstract class LogicRootObject<TUser, TRole> : RootObject<LogicDatabase<TUser, TRole>>
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		protected LogicRootObject(string name, LogicDatabase<TUser, TRole> database)
			: base(name, database)
		{
		}

		public AuditList<TUser, TRole> Audit { get; private set; }

		protected internal abstract BaseUserList<TUser, TRole> GetUsers();

		public override void Initialize()
		{
			Audit = new AuditList<TUser, TRole>(Database);
			Database.Audit = Audit;
		}
	}
}
