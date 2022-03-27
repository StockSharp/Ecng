namespace Ecng.Serialization
{
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	public class UnitializedEntityFactory<TEntity> : EntityFactory<TEntity>
	{
		public override bool FullInitialize => false;

		public override Task<TEntity> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			return ((TEntity)serializer.Type.CreateUnitialized()).FromResult();
		}
	}
}