namespace Lzma
{
	internal static class Helper
	{
		#region Methods

		/// <summary>
		/// Initializes an array of bit decoders.
		/// </summary>
		/// <param name="decoders">The decoders to initialize.</param>
		public static void InitializeAll(this BitDecoder[] decoders)
		{
			for (int i = 0; i < decoders.Length; i++)
				decoders[i].Initialize();
		}

		/// <summary>
		/// Initializes an array of bit encoder.
		/// </summary>
		/// <param name="encoders">The decoders to initialize.</param>
		public static void InitializeAll(this BitEncoder[] encoders)
		{
			for (int i = 0; i < encoders.Length; i++)
				encoders[i].Initialize();
		}

		#endregion
	}
}
