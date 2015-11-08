namespace Ecng.Forum.Components
{
	#region Using Directives

	using System;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Web.UI.WebControls;
	using System.Web.Configuration;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Forum.BusinessEntities;
	using Ecng.Forum.Components.Configuration;
	using Ecng.Logic.BusinessEntities;

	#endregion

	[DefaultProperty("TypeId")]
	public class ForumQueryStringParameter : QueryStringParameter
	{
		private readonly static Dictionary<string, string> _entityTypes = new Dictionary<string, string>();

		static ForumQueryStringParameter()
		{
			var section = ConfigManager.GetSection<ForumSection>();

			if (section != null)
			{
				foreach (AssemblyInfo asmInfo in section.EntityAssemblies)
				{
					var asm = Assembly.Load(asmInfo.Assembly);

					foreach (var type in asm.GetExportedTypes())
					{
						if (!type.IsAbstract && (type == typeof(ForumUser) || type == typeof(ForumRole) || type.IsSubclassOf(typeof(ForumBaseEntity))))
							AddEntityType(type);
					}
				}
			}
		}

		#region ForumQueryStringParameter.ctor()

		public ForumQueryStringParameter()
		{
			Type = TypeCode.Int64;
		}

		#endregion

		#region TypeId

		public string TypeId
		{
			get { return Name; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException();

				Name = value;
				//QueryStringField = ForumHelper.GetIdentity(Converter.ChangeType<Type>(this.FormattingString.Format(value.Replace("Id", string.Empty))));
				QueryStringField = _entityTypes[value];
			}
		}

		#endregion

		private static void AddEntityType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			//if (!type.IsSubclassOf(typeof(ForumBaseEntity)))
			//	throw new ArgumentException("type");

			_entityTypes.Add(type.Name + "Id", WebHelper.GetIdentity(type));
		}
	}
}