namespace Ecng.Forum.BusinessEntities
{
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Data;

	public abstract class BaseNamedEntityList<E> : BaseEntityList<E>
		where E : BaseNamedEntity
	{
		private readonly static Field _nameField = Schema.GetSchema<E>().Fields["Name"];

		protected BaseNamedEntityList(Database database)
			: base(database)
		{
		}

		public E ReadByName([Length(512)]string name)
		{
			return base.Read(_nameField, name);
		}
	}
}