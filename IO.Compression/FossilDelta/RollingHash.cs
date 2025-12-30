using System;

namespace Ecng.IO.Fossil
{
	class RollingHash
	{
		private UInt16 a;
		private UInt16 b;
		private UInt16 i;
		private byte[] z;

		public RollingHash ()
		{
			this.a = 0;
			this.b = 0;
			this.i = 0;
			this.z = new byte[Delta.NHASH];
		}

		/// <summary>
		/// Initialize the rolling hash using the first NHASH characters of z[].
		/// </summary>
		public unsafe void Init (byte* z, int pos)
		{
			UInt16 a = 0, b = 0, i, x;
			for(i = 0; i < Delta.NHASH; i++){
				x = z[pos+i];
				a = (UInt16) ((a + x) & 0xffff);
				b = (UInt16) ((b + (Delta.NHASH-i)*x) & 0xffff);
				this.z[i] = (byte) x;
			}
			this.a = (UInt16) (a & 0xffff);
			this.b = (UInt16) (b & 0xffff);
			this.i = 0;
		}

		/// <summary>
		/// Advance the rolling hash by a single character.
		/// </summary>
		public void Next (byte c) {
			UInt16 old = this.z[this.i];
			this.z[this.i] = c;
			this.i = (UInt16) ((this.i+1)&(Delta.NHASH-1));
			this.a = (UInt16) (this.a - old + c);
			this.b = (UInt16) (this.b - Delta.NHASH*old + this.a);
		}


		/// <summary>
		/// Return a 32-bit hash value.
		/// </summary>
		public UInt32 Value () {
			return (UInt32) (((UInt32)(this.a & 0xffff)) | (((UInt32)(this.b & 0xffff)) << 16));
		}

		/// <summary>
		/// Compute a hash on NHASH bytes in a single call.
		/// </summary>
		public static UInt32 Once (byte[] z) {
			UInt16 a, b, i;
			a = b = z[0];
			for(i=1; i<Delta.NHASH; i++){
				a += z[i];
				b += a;
			}
			return a | (((UInt32)b)<<16);
		}
			
	}
}

