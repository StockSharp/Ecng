namespace Ecng.Tests.Interop;

using System.Runtime.InteropServices;
using System.Text;

using Ecng.Interop;

[TestClass]
public class MarshalerTests : BaseTestClass
{
	[TestMethod]
	public void ToString_Utf8Multibyte_DecodesFully()
	{
		var value = "Привет, мир!";
		var bytes = Encoding.UTF8.GetBytes(value);

		// NUL-terminated buffer.
		var ptr = Marshal.AllocHGlobal(bytes.Length + 1);

		try
		{
			Marshal.Copy(bytes, 0, ptr, bytes.Length);
			Marshal.WriteByte(ptr, bytes.Length, 0);

			var result = Encoding.UTF8.ToString(ptr);
			result.AssertEqual(value);
		}
		finally
		{
			Marshal.FreeHGlobal(ptr);
		}
	}

	[TestMethod]
	public void ToString_ZeroPointer_ReturnsNull()
	{
		var result = Encoding.UTF8.ToString(IntPtr.Zero);
		result.AssertNull();
	}

	[TestMethod]
	public void GetUnsafeString_TerminatedWithinBuffer_TrimsTail()
	{
		var value = "abc";
		var bytes = Encoding.ASCII.GetBytes(value);
		const int maxBytes = 16;

		// Allocate a larger NUL-padded buffer to emulate a fixed-size native field.
		var buffer = new byte[maxBytes];
		Array.Copy(bytes, buffer, bytes.Length);

		unsafe
		{
			fixed (byte* p = buffer)
			{
				var result = Encoding.ASCII.GetUnsafeString(ref *p, maxBytes);
				result.AssertEqual(value);
			}
		}
	}
}
