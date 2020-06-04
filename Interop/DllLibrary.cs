namespace Ecng.Interop
{
	using System;
	using System.Diagnostics;

	using Ecng.Common;

	public abstract class DllLibrary : Disposable
	{
		protected DllLibrary(string dllPath)
		{
			if (dllPath.IsEmpty())
				throw new ArgumentNullException(nameof(dllPath));

			DllPath = dllPath;
			Handler = Marshaler.LoadLibrary(dllPath);

			DllVersion = FileVersionInfo.GetVersionInfo(dllPath).ProductVersion?.Replace(',', '.')?.RemoveSpaces()?.To<Version>();
		}

		public string DllPath { get; private set; }

		public Version DllVersion { get; private set; }

		protected IntPtr Handler { get; }

		protected T GetHandler<T>(string procName) => Handler.GetHandler<T>(procName);

		protected T TryGetHandler<T>(string procName)
			where T : Delegate
			=> Handler.TryGetHandler<T>(procName);

		protected override void DisposeNative()
		{
			Handler.FreeLibrary();
			base.DisposeNative();
		}
	}
}