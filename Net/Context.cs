namespace Ecng.Net
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;

	using Ecng.Reflection;
	using Ecng.Serialization;
	using Ecng.Collections;

	class Context<T>
	{
		public Context(FastInvoker<T, object, object> invoker, Tuple<ParameterInfo, ISerializer, ParamConverterAttribute>[] argDeserializers, ISerializer returnSerializer, bool isCached, bool isCompressed)
		{
			Invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
			ArgDeserializers = argDeserializers ?? throw new ArgumentNullException(nameof(argDeserializers));
			ReturnSerializer = returnSerializer;
			IsCached = isCached;
			IsCompressed = isCompressed;

			Func<object[], object[], bool> comparer = (args1, ags2) => args1.SequenceEqual(ags2, ReferenceEquals);
			_cache = new Dictionary<object[], Stream>(comparer.ToComparer());
		}

		public FastInvoker<T, object, object> Invoker { get; }
		public Tuple<ParameterInfo, ISerializer, ParamConverterAttribute>[] ArgDeserializers { get; }
		public ISerializer ReturnSerializer { get; }
		public bool IsCached { get; }
		public bool IsCompressed { get; }

		private readonly Dictionary<object[], Stream> _cache;

		public Stream GetCache(object[] args)
		{
			lock (_cache)
				return _cache.TryGetValue(args);
		}

		public void AddCache(object[] args, Stream data)
		{
			lock (_cache)
				_cache[args] = data;
		}
	}
}