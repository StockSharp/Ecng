namespace Ecng.Interop
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics;

	using Ecng.Common;

	public abstract class NativeLibrary : Disposable
	{
		protected NativeLibrary(string dllPath)
		{
			if (dllPath.IsEmpty())
				throw new ArgumentNullException(nameof(dllPath));

			DllPath = dllPath;

			DllVersion = FileVersionInfo.GetVersionInfo(dllPath).ProductVersion.Replace(',', '.').Replace(" ", "").To<Version>();

			Handler = WinApi.LoadLibrary(dllPath);

			if (Handler == IntPtr.Zero)
				throw new ArgumentException("Ошибка в загрузке библиотеки {0}.".Put(dllPath), nameof(dllPath), new Win32Exception());
		}

		public string DllPath { get; private set; }

		public Version DllVersion { get; private set; }

		protected IntPtr Handler { get; }

		protected T GetHandler<T>(string procName)
		{
			var addr = Marshaler.GetProcAddress(Handler, procName);

			if (addr == IntPtr.Zero)
				throw new ArgumentException("Ошибка в загрузке процедуры {0}.".Put(procName), nameof(procName), new Win32Exception());

			return Marshaler.GetDelegateForFunctionPointer<T>(addr);
		}

		protected override void DisposeNative()
		{
			WinApi.FreeLibrary(Handler);
			base.DisposeNative();
		}
	}
}