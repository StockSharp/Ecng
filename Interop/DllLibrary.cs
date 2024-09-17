namespace Ecng.Interop
{
	using System;
	using System.Diagnostics;

	using Ecng.Common;

	public abstract class DllLibrary(string dllPath) : Disposable
	{
		public string DllPath { get; private set; } = dllPath.ThrowIfEmpty(nameof(dllPath));

		private Version _dllVersion;
		public Version DllVersion => _dllVersion ??= FileVersionInfo.GetVersionInfo(DllPath).ProductVersion?.Replace(',', '.')?.RemoveSpaces()?.To<Version>();

		protected IntPtr Handler { get; } = Marshaler.LoadLibrary(dllPath);

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