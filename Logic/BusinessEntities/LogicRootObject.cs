namespace Ecng.Logic.BusinessEntities
{
	using Ecng.Data;
	using Ecng.Web;

	public interface IUserList
	{
		IWebUser Null { get; }
		IWebUser ReadByName(string name);
	}

	public abstract class LogicRootObject : RootObject<LogicDatabase>
	{
		protected LogicRootObject(string name, LogicDatabase database)
			: base(name, database)
		{
		}

		public AuditList Audit { get; private set; }

		protected internal abstract IUserList GetUsers();

		public override void Initialize()
		{
			Audit = new AuditList(Database);
			Database.Audit = Audit;
		}
	}
}
