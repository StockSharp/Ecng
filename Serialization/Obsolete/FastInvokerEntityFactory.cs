namespace Ecng.Serialization
{
	using System.Reflection;

	using Ecng.Reflection;

	public class FastInvokerEntityFactory<E> : EntityFactory<E>
	{
		private static readonly FastInvoker<VoidType, VoidType, E> _invoker;

		static FastInvokerEntityFactory()
		{
			_invoker = FastInvoker<VoidType, VoidType, E>.Create(typeof(E).GetMember<ConstructorInfo>());
		}

		public override bool FullInitialize => false;

		public override E CreateEntity(ISerializer serializer, SerializationItemCollection source)
		{
			return _invoker.Ctor(null);
		}
	}
}