namespace Ecng.Drawing
{
	using System;
	using System.Drawing;
	using System.Drawing.Drawing2D;
	using System.Drawing.Imaging;
	using System.IO;

	using Ecng.Common;

	public static class ImageExtensions
	{
		public static bool IsWinColor(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type == typeof(Color);
		}

		public static byte[] ToPng(this byte[] image)
			=> image.ChangeImageFormat(ImageFormat.Png);

		public static byte[] ChangeImageFormat(this byte[] image, ImageFormat format)
		{
			if (image is null)
				throw new ArgumentNullException(nameof(image));

			if (format is null)
				throw new ArgumentNullException(nameof(format));

			using var img = Image.FromStream(image.To<Stream>());

			if (img.RawFormat.Equals(format))
				return image;

			var stream = new MemoryStream();
			img.Save(stream, ImageFormat.Png);
			return stream.To<byte[]>();
		}

		public static byte[] ShrinkImage(this byte[] file, (int width, int height) size)
		{
			if (file is null)
				throw new ArgumentNullException(nameof(file));

			//if (size is null)
			//	throw new ArgumentNullException(nameof(size));

			var body = file.To<Stream>();

			if (body is null)
				throw new ArgumentException("file");

			if (size != default)
			{
				if (size.width <= 0)
					throw new ArgumentOutOfRangeException(nameof(size.width));

				if (size.height <= 0)
					throw new ArgumentOutOfRangeException(nameof(size.height));

				using var srcImage = new Bitmap(body);

				var coeff = (double)srcImage.Width / size.width;

				if (coeff <= 1)
					coeff = (double)srcImage.Height / size.height;

				if (coeff > 1)
				{
					var newWidth = (int)(srcImage.Width / coeff);
					var newHeight = (int)(srcImage.Height / coeff);

					body = new MemoryStream();

					using var newImage = new Bitmap(newWidth, newHeight);

					using (var gr = Graphics.FromImage(newImage))
					{
						gr.SmoothingMode = SmoothingMode.HighQuality;
						gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
						gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
						gr.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
					}

					newImage.Save(body, ImageFormat.Png);
				}
			}

			return body.To<byte[]>();
		}
	}
}