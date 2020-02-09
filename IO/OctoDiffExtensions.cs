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
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			var signatureBuilder = new SignatureBuilder();

			using (var basisStream = new MemoryStream(file))
			using (var signatureStream = new MemoryStream())
			{
				signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
				signatureStream.Position = 0;
				var result = signatureStream.ToArray();
				return result;
			}
		}

		public static byte[] CreateDelta(this byte[] signature, byte[] newFile)
		{
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));

			if (newFile == null)
				throw new ArgumentNullException(nameof(newFile));

			var deltaBuilder = new DeltaBuilder();

			using (var newFileStream = new MemoryStream(newFile))
			using (var signatureFileStream = new MemoryStream(signature))
			using (var deltaStream = new MemoryStream())
			{
				deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureFileStream, _nullReporter), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
				deltaStream.Position = 0;
				var result = deltaStream.ToArray();
				return result;
			}
		}

		public static byte[] CreateOriginal(this byte[] signature, byte[] delta)
		{
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));

			if (delta == null)
				throw new ArgumentNullException(nameof(delta));

			var deltaApplier = new DeltaApplier { SkipHashCheck = true };

			using (var basisStream = new MemoryStream(signature))
			using (var deltaStream = new MemoryStream(delta))
			using (var newFileStream = new MemoryStream())
			{
				deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, _nullReporter), newFileStream);
				newFileStream.Position = 0;
				var result = newFileStream.ToArray();
				return result;
			}
		}
	}
}