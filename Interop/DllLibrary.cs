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
			DllVersion = FileVersionInfo.GetVersionInfo(DllPath).ProductVersion?.Replace(',', '.')?.RemoveSpaces()?.To<Version>();

			Handler = Marshaler.LoadLibrary(DllPath);
		}

		public string DllPath { get; private set; }

		public Version DllVersion { get; private set; }

		protected IntPtr Handler { get; }

		protected T GetHandler<T>(string procName) => Marshaler.GetDelegateForFunctionPointer<T>(Marshaler.GetProcAddress(Handler, procName));

		protected override void DisposeNative()
		{
			Marshaler.FreeLibrary(Handler);
			base.DisposeNative();
		}

	}
}