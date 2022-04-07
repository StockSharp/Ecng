namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Collections;

	#endregion

	[Serializable]
	class FieldFactoryChain : FieldFactory
	{
		public FieldFactoryChain(ICollection<FieldFactory> factories, Field field)
			: base(field, 0)
		{
			if (factories is null)
				throw new ArgumentNullException(nameof(factories));

			if (factories.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(factories));

			AscFactories = factories.OrderBy(factory => factory.Order);
			DescFactories = AscFactories.Reverse();
		}

		public override Type InstanceType => AscFactories.Last().InstanceType;

		public override Type SourceType => AscFactories.First().SourceType;

		public IEnumerable<FieldFactory> AscFactories { get; }
		public IEnumerable<FieldFactory> DescFactories { get; }

		#region FieldFactory Members

		protected internal override async ValueTask<object> OnCreateInstance(ISerializer serializer, object source, CancellationToken cancellationToken)
		{
			var instance = source;

			foreach (var factory in AscFactories)
			{
				instance = await factory.OnCreateInstance(serializer, instance, cancellationToken);

				if (instance is null)
					break;
			}

			return instance;
		}

		protected internal override async ValueTask<object> OnCreateSource(ISerializer serializer, object instance, CancellationToken cancellationToken)
		{
			var source = instance;

			foreach (var factory in DescFactories)
			{
				source = await factory.OnCreateSource(serializer, source, cancellationToken);

				if (source is null)
					break;
			}

			return source;
		}

		#endregion
	}
}