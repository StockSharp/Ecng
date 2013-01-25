namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	#endregion

	[Serializable]
	class FieldFactoryChain : FieldFactory
	{
		public FieldFactoryChain(ICollection<FieldFactory> factories, Field field)
			: base(field, 0)
		{
			if (factories == null)
				throw new ArgumentNullException("factories");

			if (factories.IsEmpty())
				throw new ArgumentOutOfRangeException("factories");

			AscFactories = factories.OrderBy(factory => factory.Order);
			DescFactories = AscFactories.Reverse();
		}

		public override Type InstanceType
		{
			get { return AscFactories.Last().InstanceType; }
		}

		public override Type SourceType
		{
			get { return AscFactories.First().SourceType; }
		}

		public IEnumerable<FieldFactory> AscFactories { get; private set; }
		public IEnumerable<FieldFactory> DescFactories { get; private set; }

		#region FieldFactory Members

		protected internal override object OnCreateInstance(ISerializer serializer, object source)
		{
			var instance = source;

			foreach (var factory in AscFactories)
			{
				instance = factory.OnCreateInstance(serializer, instance);

				if (instance == null)
					break;
			}

			return instance;
		}

		protected internal override object OnCreateSource(ISerializer serializer, object instance)
		{
			var source = instance;

			foreach (var factory in DescFactories)
			{
				source = factory.OnCreateSource(serializer, source);

				if (source == null)
					break;
			}

			return source;
		}

		#endregion
	}
}