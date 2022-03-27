namespace Ecng.Serialization
{
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Reflection;
	using Ecng.Common;

	public class FastInvokerEntityFactory<E> : EntityFactory<E>
	{
		private static readonly FastInvoker<VoidType, VoidType, E> _invoker;

		static FastInvokerEntityFactory()
		{
			_invoker = FastInvoker<VoidType, VoidType, E>.Create(typeof(E).GetMember<ConstructorInfo>());
		}

		public override bool FullInitialize => false;

		public override Task<E> CreateEntity(ISerializer serializer, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			return _invoker.Ctor(null).FromResult();
		}
	}
}