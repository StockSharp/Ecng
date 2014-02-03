namespace Ecng.Localization
{
    #region Using Directives

	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;

	using Ecng.Common;

	#endregion

	public static class LocalizationHelper
	{
		#region GetResource

		public static Stream GetResource(this string name)
        {
            var type = GetCallingType();
			return "{0}.Resources.{1}".Put(type.Namespace, name).GetResource(type.Assembly);
        }

		public static Stream GetResource(this string name, Assembly assembly)
        {
            return assembly.GetManifestResourceStream(name);
        }

        #endregion

        #region GetString

		public static string Localize(this string resName)
        {
			return resName.Localize(new object[0]);
        }

		public static string Localize(this string resName, params object[] additionalArgs)
        {
			return resName.Localize(GetCallingType(), additionalArgs);
        }

		public static string Localize(this string resName, Type resType)
        {
			return resName.Localize(resType, new object[0]);
        }

        public static string Localize(this string resName, Type resType, params object[] additionalArgs)
        {
			if (resType == null)
				throw new ArgumentNullException("resName");

			if (resName.IsEmpty())
				throw new ArgumentNullException("resType");

            using (var manager = new DisposableResourceManager(resType))
            {
				string retVal;

				try
				{
					retVal = manager.GetString(resName);
				}
				catch
				{
					retVal = resName;
				}

				return retVal.Put(additionalArgs);
            }
        }

        #endregion

		#region GetResourceObject

		public static object GetResourceObject(this string resName)
        {
			return resName.GetResourceObject(GetCallingType());
        }

		public static object GetResourceObject(this string resName, Type resType)
        {
            if (resType == null)
                throw new ArgumentNullException("resType");

			if (resName.IsEmpty())
                throw new ArgumentNullException("resName");

            using (var manager = new DisposableResourceManager(resType))
                return manager.GetObject(resName);
        }

        #endregion

        #region GetCallingType

        private static Type GetCallingType()
        {
            return new StackFrame(2).GetMethod().ReflectedType;
        }

        #endregion
    }
}