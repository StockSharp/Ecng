namespace Ecng.Interop;

using System;
using System.Diagnostics;

using Ecng.Common;

/// <summary>
/// Represents a base class that manages a DLL library and provides access to its exported functions.
/// </summary>
/// <param name="dllPath">The file path to the DLL.</param>
public abstract class DllLibrary(string dllPath) : Disposable
{
	/// <summary>
	/// Gets the file path to the DLL.
	/// </summary>
	public string DllPath { get; private set; } = dllPath.ThrowIfEmpty(nameof(dllPath));

	private Version _dllVersion;

	/// <summary>
	/// Gets the version of the DLL by retrieving product information.
	/// </summary>
	public Version DllVersion => _dllVersion ??=
		FileVersionInfo.GetVersionInfo(DllPath).ProductVersion?
			.Replace(',', '.')
			?.RemoveSpaces()
			?.To<Version>();

	/// <summary>
	/// Gets the pointer to the loaded DLL.
	/// </summary>
	protected IntPtr Handler { get; } = Marshaler.LoadLibrary(dllPath);

	/// <summary>
	/// Retrieves a function pointer from the DLL and casts it to the specified delegate type.
	/// </summary>
	/// <typeparam name="T">The type of the delegate.</typeparam>
	/// <param name="procName">The name of the procedure to retrieve.</param>
	/// <returns>A delegate of type <typeparamref name="T"/>.</returns>
	protected T GetHandler<T>(string procName) => Handler.GetHandler<T>(procName);

	/// <summary>
	/// Attempts to retrieve a function pointer from the DLL as the specified delegate type. Returns null if not found.
	/// </summary>
	/// <typeparam name="T">The type of the delegate.</typeparam>
	/// <param name="procName">The name of the procedure to retrieve.</param>
	/// <returns>A delegate of type <typeparamref name="T"/>, or null if the procedure is not found.</returns>
	protected T TryGetHandler<T>(string procName)
		where T : Delegate
		=> Handler.TryGetHandler<T>(procName);

	/// <summary>
	/// Disposes native resources associated with the DLL.
	/// </summary>
	protected override void DisposeNative()
	{
		Handler.FreeLibrary();
		base.DisposeNative();
	}
}