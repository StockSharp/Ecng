namespace Ecng.Logic.BusinessEntities
{
	using Ecng.Data;

	public interface IUserList<TUser, TRole>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		TUser Null { get; }
		TUser ReadByName(string name);
	}

	public abstract class LogicRootObject<TUser, TRole> : RootObject<LogicDatabase<TUser, TRole>>
		where TUser : BaseEntity<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		protected LogicRootObject(string name, LogicDatabase<TUser, TRole> database)
			: base(name, database)
		{
		}

		public AuditList<TUser, TRole> Audit { get; private set; }

		protected internal abstract IUserList<TUser, TRole> GetUsers();

		public override void Initialize()
		{
			Audit = new AuditList<TUser, TRole>(Database);
			Database.Audit = Audit;
		}
	}
}
