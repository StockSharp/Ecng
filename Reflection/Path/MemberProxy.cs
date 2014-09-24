namespace Ecng.Reflection.Path
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Deployment.Internal;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;
	using System.Text.RegularExpressions;

	using Ecng.Collections;
	using Ecng.Common;

	#endregion

	public class MemberProxy : MethodInfo
	{
		#region Private Fields

		private const RegexOptions _options = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled;

		private readonly static Regex _methodRegex = new Regex(@"(?<methodName>\w+) \( (?<inRefArgs>[^\)]*) \)", _options);
		private readonly static Regex _indexerRegex = new Regex(@"(?<propName>\w+) \[ (?<inRefArgs>[^\]]+) \]", _options);
		private readonly static Regex _propRegex = new Regex(@"(?<propName>\w+)(\.|$)", _options);
		//private static Regex _argsRegex = new Regex(@"(\@*) \w+", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

		private readonly static Dictionary<Tuple<Type, string>, MemberProxy> _proxies = new Dictionary<Tuple<Type, string>, MemberProxy>();

		private readonly Type _rootType;
		private readonly string _path;

		#endregion

		#region MemberProxy.ctor()

		private MemberProxy(Type rootType, string path, IEnumerable<MemberProxyItem> items)
		{
			_rootType = rootType;
			_path = path;

			if (items.HasNullItem())
				throw new ArgumentException("items");

			Items = items;
		}

		#endregion

		public bool ThrowOnNull { get; set; }

		#region Items

		public IEnumerable<MemberProxyItem> Items { get; private set; }

		#endregion

		#region Invoke

		public object Invoke(object instance)
		{
			return Invoke(instance, new Dictionary<string, object>());
		}

		public object Invoke(object instance, IDictionary<string, object> args)
		{
			if (args == null)
				throw new ArgumentNullException("args");

			var formatterArgs = args.ToDictionary(pair => pair.Key.ToLower(), pair => pair.Value);

			foreach (var item in Items)
			{
				if (instance == null && !item.Invoker.Member.IsStatic())
				{
					if (ThrowOnNull)
						throw new InvalidOperationException();
					else
						break;
				}

				instance = item.Invoke(instance, formatterArgs);
			}

			return instance;
		}

		#endregion

		public void SetValue(object instance, object value)
		{
			if (Items.IsEmpty())
				return;

			foreach (var item in Items.Take(Items.Count() - 1))
			{
				if (instance == null && !item.Invoker.Member.IsStatic())
				{
					if (ThrowOnNull)
						throw new InvalidOperationException();
					else
						break;
				}

				var newInstance = item.Invoke(instance, new Dictionary<string, object>());

				if (newInstance == null)
				{
					newInstance = item.Invoker.Member.GetMemberType().CreateInstance();
					item.SetValue(instance, newInstance);
				}

				instance = newInstance;
			}

			Items.Last().SetValue(instance, value);
		}

		#region Create

		public static MemberProxy Create(Type rootType, string path)
		{
			return _proxies.SafeAdd(new Tuple<Type, string>(rootType, path), delegate
			{
				return new MemberProxy(rootType, path, GetItems(rootType, path.Split('.')));
			});
		}

		#endregion

		#region GetItems

		private static IEnumerable<MemberProxyItem> GetItems(Type type, IEnumerable<string> memberNames)
		{
			var items = new List<MemberProxyItem>();

			foreach (var memberName in memberNames)
			{
				if (type == null)
					throw new ArgumentNullException("Type for member '{0}' is null.".Put(memberName), "type");

				if (type == typeof(void))
					throw new ArgumentException("Type for member '{0}' is void.".Put(memberName), "type");

				var methodMatch = _methodRegex.Match(memberName);
				var indexerMatch = _indexerRegex.Match(memberName);
				var propMatch = _propRegex.Match(memberName);

				if (methodMatch.Success)
				{
					var methods = type.GetMembers<MethodInfo>(ReflectionHelper.AllMembers, true, methodMatch.Groups["methodName"].Value);

					var parametersSet = methods.Select(m => m.GetParameters()).ToList();

					MethodInfo method;
					var @params = GetParams(methodMatch, parametersSet, out method);

					if (methods.Length == 1)
						method = methods[0];

					items.Add(new MethodProxyItem(method, @params));
					type = method.ReturnType;
				}
				else if (indexerMatch.Success)
				{
					var member = type.GetMember<MemberInfo>(indexerMatch.Groups["propName"].Value);
					items.Add(new FieldPropProxyItem(member));

					var indexers = member.GetMemberType().GetIndexers();

					var parametersSet = indexers.Select(i => i.GetGetMethod(true).GetParameters()).ToList();

					MethodInfo indexer;
					var @params = GetParams(indexerMatch, parametersSet, out indexer);

					if (indexers.Length == 1)
						indexer = indexers[0].GetGetMethod(true);

					items.Add(new IndexerProxyItem(indexer, @params));
					type = indexer.ReturnType;
				}
				else if (propMatch.Success)
				{
					var member = type.GetMember<MemberInfo>(propMatch.Groups["propName"].Value);
					items.Add(new FieldPropProxyItem(member));
					type = member.GetMemberType();
				}
				else
					throw new ArgumentException("memberNames");
			}

			return items;
		}

		#endregion

		#region GetParams

		private static IEnumerable<Param> GetParams<T>(Match match, IList<ParameterInfo[]> parametersSet, out T member)
			where T : MemberInfo
		{
			if (parametersSet == null)
				throw new ArgumentNullException("parametersSet");

			var @params = new List<Param>();

			var argsGroup = match.Groups["inRefArgs"].Value;

			if (!argsGroup.IsEmpty())
			{
				if (parametersSet.IsEmpty())
					throw new ArgumentOutOfRangeException("parametersSet");

				var args = argsGroup.Split(',');

				// remove unsuitable singnature parameters
				for (int i = 0; i < parametersSet.Count; i++)
				{
					var parameters = parametersSet[i];

					if (parameters.Length != args.Length)
					{
						parametersSet.RemoveAt(i);
						i--;
					}
				}

				if (parametersSet.IsEmpty())
					throw new ArgumentException("parametersSet");

				int index = 0;

				foreach (var arg in args)
				{
					var parts = arg.Trim().Split(new [] { " as " }, StringSplitOptions.RemoveEmptyEntries);

					if (parts.IsEmpty())
						throw new ArgumentException("match");

					var paramName = parts[0];
					Type paramType;

					if (parts.Length == 2)
						paramType = parts[1].To<Type>();
					else if (!paramName.Contains("@") && paramName.Contains("'"))
						paramType = typeof(string);
					else
						paramType = typeof(void);

					if (paramType != typeof(void))
					{
						for (int i = 0; i < parametersSet.Count; i++)
						{
							var parameters = parametersSet[i];

							if (parameters[index].ParameterType != paramType)
							{
								parametersSet.RemoveAt(i);
								i--;
							}
						}

						if (parametersSet.IsEmpty())
							throw new ArgumentException("parametersSet");
					}

					var param = new Param();

					if (paramName.Contains("@"))
					{
						param.Name = paramName.Replace("@", "").ToLower();
						param.Type = ParamType.Reference;
					}
					else
					{
						param.Value = paramName.Replace("'", "");
						param.Type = ParamType.Direct;
					}

					@params.Add(param);

					index++;
				}

				index = 0;
				foreach (var param in @params)
				{
					param.Info = parametersSet[0][index++];
					param.Value = param.Value.To(param.Info.ParameterType);
				}

				member = parametersSet[0][0].Member.To<T>();
			}
			else
				member = null;

			return @params;
		}

		#endregion

		#region MethodInfo Members

		public override MethodInfo GetBaseDefinition()
		{
			return null;
		}

		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get { return null; }
		}

		public override MethodAttributes Attributes
		{
			get { return MethodAttributes.Public; }
		}

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return MethodImplAttributes.Managed;
		}

		public override ParameterInfo[] GetParameters()
		{
			return ArrayHelper<ParameterInfo>.EmptyArray;
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return Invoke(obj);
		}

		public override RuntimeMethodHandle MethodHandle
		{
			get { return default(RuntimeMethodHandle); }
		}

		public override Type DeclaringType
		{
			get { return _rootType; }
		}

		public override string Name
		{
			get { return _path; }
		}

		public override Type ReflectedType
		{
			get { return _rootType; }
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return ArrayHelper<object>.EmptyArray;
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return ArrayHelper<object>.EmptyArray;
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return false;
		}

		public override Type ReturnType
		{
			get { return Items.IsEmpty() ? typeof(void) : Items.Last().Invoker.Member.GetMemberType(); }
		}

		#endregion
	}
}