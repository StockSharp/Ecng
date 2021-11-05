namespace Ecng.IO
{
	using System;
	using System.IO;

	using Octodiff.Core;
	using Octodiff.Diagnostics;

	public static class OctoDiffExtensions
	{
		private static readonly IProgressReporter _nullReporter = new NullProgressReporter();

		public static byte[] CreateSignature(this byte[] file)
		{
			if (file is null)
				throw new ArgumentNullException(nameof(file));

			using var basisStream = new MemoryStream(file);
			using var signatureStream = new MemoryStream();
			basisStream.CreateSignature(signatureStream);
			signatureStream.Position = 0;
			return signatureStream.ToArray();
		}

		public static void CreateSignature(this Stream file, Stream output)
		{
			if (file is null)
				throw new ArgumentNullException(nameof(file));

			new SignatureBuilder().Build(file, new SignatureWriter(output));
		}

		public static byte[] CreateDelta(this byte[] signature, byte[] newFile)
		{
			if (signature is null)
				throw new ArgumentNullException(nameof(signature));

			if (newFile is null)
				throw new ArgumentNullException(nameof(newFile));

			using var newFileStream = new MemoryStream(newFile);
			using var signatureFileStream = new MemoryStream(signature);
			using var deltaStream = new MemoryStream();
			signatureFileStream.CreateDelta(newFileStream, deltaStream);
			deltaStream.Position = 0;
			return deltaStream.ToArray();
		}

		public static void CreateDelta(this Stream signature, Stream newFile, Stream output)
		{
			if (signature is null)
				throw new ArgumentNullException(nameof(signature));

			if (newFile is null)
				throw new ArgumentNullException(nameof(newFile));

			new DeltaBuilder().BuildDelta(newFile, new SignatureReader(signature, _nullReporter), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(output)));
		}

		public static byte[] CreateOriginal(this byte[] signature, byte[] delta, bool checkHash = false)
		{
			if (signature is null)
				throw new ArgumentNullException(nameof(signature));

			if (delta is null)
				throw new ArgumentNullException(nameof(delta));

			using var basisStream = new MemoryStream(signature);
			using var deltaStream = new MemoryStream(delta);
			using var output = new MemoryStream();
			basisStream.CreateOriginal(deltaStream, output, checkHash);
			output.Position = 0;
			return output.ToArray();
		}

		public static void CreateOriginal(this Stream basis, Stream delta, Stream output, bool checkHash = false)
		{
			if (basis is null)
				throw new ArgumentNullException(nameof(basis));

			if (delta is null)
				throw new ArgumentNullException(nameof(delta));

			new DeltaApplier { SkipHashCheck = checkHash }
				.Apply(basis, new BinaryDeltaReader(delta, _nullReporter), output);
		}
	}
}