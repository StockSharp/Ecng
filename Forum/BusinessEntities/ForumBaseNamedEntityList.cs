namespace Ecng.Forum.BusinessEntities
{
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	public abstract class ForumBaseNamedEntityList<TEntity> : ForumBaseEntityList<TEntity>
		where TEntity : ForumBaseNamedEntity
	{
		protected ForumBaseNamedEntityList(IStorage storage)
			: base(storage)
		{
		}

		private readonly static Field _nameField = SchemaManager.GetSchema<TEntity>().Fields["Name"];

		public TEntity ReadByName([Length(512)]string name)
		{
			return Read(_nameField, name);
		}
	}
}