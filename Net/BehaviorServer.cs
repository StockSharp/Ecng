namespace Ecng.Net
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Serialization;

	using ICSharpCode.SharpZipLib.GZip;

	public abstract class BehaviorServer<TBehavior, TId> : Disposable
	{
		#region Private Fields

		private static readonly Dictionary<TId, Context<TBehavior>> _contexts = new Dictionary<TId, Context<TBehavior>>();

		#endregion

		#region BehaviorServer.cctor()

		static BehaviorServer()
		{
			foreach (var method in typeof(TBehavior).GetMembers<MethodInfo>())
			{
				var attr = method.GetAttribute<RemoteMethodAttribute>();

				if (attr != null)
				{
					var invoker = FastInvoker<TBehavior, object, object>.Create(method);

					var argDeserializers = new List<Tuple<ParameterInfo, ISerializer, ParamConverterAttribute>>();

					foreach (var parameter in method.GetParameters())
					{
						var paramAttr = parameter.GetAttribute<ParamConverterAttribute>();
						Type paramType;
						if (paramAttr != null)
						{
							paramType = paramAttr.SourceType;
							paramAttr.Init(parameter.ParameterType);
						}
						else
						{
							paramType = parameter.ParameterType;
							paramAttr = new DefaultParamConverterAttribute(paramType);
						}

						argDeserializers.Add(new Tuple<ParameterInfo, ISerializer, ParamConverterAttribute>(parameter, CreateSerializer(paramType), paramAttr));
					}

					ISerializer returnSerializer = null;

					if (method.ReturnType != typeof(void))
						returnSerializer = CreateSerializer(method.ReturnType);

					var methodId = (TId)attr.GetId(method);

					if (_contexts.ContainsKey(methodId))
						throw new InvalidOperationException("Method '{0}' with id '{1}' already added.".Put(method.Name, methodId));

					_contexts.Add(methodId, new Context<TBehavior>(invoker, argDeserializers.ToArray(), returnSerializer, attr.IsCached, attr.IsCompressed));
				}
			}
		}

		#endregion

		#region BehaviorServer.ctor()

		protected BehaviorServer(TBehavior behavior)
		{
			if (behavior.IsNull())
				throw new ArgumentNullException(nameof(behavior));

			Behavior = behavior;
		}

		#endregion

		public TBehavior Behavior { get; }

		protected abstract IEnumerable<Field> Filter(IEnumerable<Field> fields);

		#region CreateSerializer

		private static ISerializer CreateSerializer(Type type)
		{
			return typeof(BinarySerializer<>).Make(type).CreateInstance<ISerializer>();
		}

		#endregion

		public Stream Invoke(TId contextId, Stream input)
		{
			try
			{
				var context = _contexts[contextId];

				object[] args;

				using (new Scope<SerializationContext>(new SerializationContext { Filter = Filter }))
					args = context.ArgDeserializers.Select(deserializer => deserializer.Item2.Deserialize(input)).ToArray();

				var cache = context.GetCache(args);

				if (cache != null)
					return cache;
				else
				{
					for (var i = 0; i < args.Length; i++)
						args[i] = context.ArgDeserializers[i].Item3.Convert(args[i]);
				}

				var invoker = context.Invoker;

				if (context.ReturnSerializer != null)
				{
					var retVal = args.Length == 1 ? invoker.ReturnInvoke(Behavior, args[0]) : invoker.ReturnInvoke(Behavior, args);

					using (new Scope<SerializationContext>(new SerializationContext { Filter = Filter }))
					{
						var data = new MemoryStream();
						context.ReturnSerializer.Serialize(retVal, data);

						if (context.IsCompressed)
						{
							data.Position = 0;

							var stream = new MemoryStream();

							using (var outStream = new GZipOutputStream(stream) { IsStreamOwner = false })
								data.CopyTo(outStream);

							data = stream;
						}

						data.Position = 0;

						if (context.IsCached)
							context.AddCache(args, data);

						return data;
					}
				}
				else
				{
					invoker.VoidInvoke(Behavior, args.Length == 1 ? args[0] : args);
                    return new MemoryStream();
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Processing '{0}' throws exception.".Put(contextId), nameof(input), ex);
			}
		}
	}
}