namespace Ecng.Serialization
{
	using Ecng.Common;

	public class UnitializedEntityFactory<TEntity> : EntityFactory<TEntity>
	{
		public override bool FullInitialize { get { return false; } }

		public override TEntity CreateEntity(ISerializer serializer, SerializationItemCollection source)
		{
			return (TEntity)serializer.Type.CreateUnitialized();
		}
	}
}