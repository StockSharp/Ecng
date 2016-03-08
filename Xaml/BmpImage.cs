namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	using Ecng.Common;

	// http://blogs.msdn.com/b/dwayneneed/archive/2007/10/05/blurry-bitmaps.aspx
	public class BmpImage : FrameworkElement
	{
		private readonly EventHandler _sourceDownloaded;
		private readonly EventHandler<ExceptionEventArgs> _sourceFailed;
		private Point _pixelOffset;

		public BmpImage()
		{
			_sourceDownloaded = OnSourceDownloaded;
			_sourceFailed = OnSourceFailed;

			LayoutUpdated += OnLayoutUpdated;
		}

		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(BitmapSource), typeof(BmpImage),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure, OnSourceChanged));

		public BitmapSource Source
		{
			get
			{
				return (BitmapSource)GetValue(SourceProperty);
			}
			set
			{
				SetValue(SourceProperty, value);
			}
		}

		public event EventHandler<ExceptionEventArgs> BitmapFailed;

		// Return our measure size to be the size needed to display the bitmap pixels.
		protected override Size MeasureOverride(Size availableSize)
		{
			var measureSize = new Size();

			var bitmapSource = Source;

			if (bitmapSource != null)
			{
				var ps = PresentationSource.FromVisual(this);

				if (ps != null && ps.CompositionTarget != null)
				{
					var fromDevice = ps.CompositionTarget.TransformFromDevice;

					var pixelSize = new Vector(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
					var measureSizeV = fromDevice.Transform(pixelSize);
					measureSize = new Size(measureSizeV.X, measureSizeV.Y);
				}
				else
				{
					measureSize = new Size(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
				}
			}

			return measureSize;
		}

		protected override void OnRender(DrawingContext dc)
		{
			var bitmapSource = Source;
			if (bitmapSource != null)
			{
				_pixelOffset = GetPixelOffset();

				var desiredSize = new Size(DesiredSize.Width - Margin.Left - Margin.Right, DesiredSize.Height - Margin.Top - Margin.Bottom);

				// Render the bitmap offset by the needed amount to align to pixels.
				dc.DrawImage(bitmapSource, new Rect(_pixelOffset, desiredSize));
			}
		}

		private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var bitmap = (BmpImage)d;

			var oldValue = (BitmapSource)e.OldValue;
			var newValue = (BitmapSource)e.NewValue;

			if (((oldValue != null) && (bitmap._sourceDownloaded != null)) && !oldValue.IsFrozen)
			{
				oldValue.DownloadCompleted -= bitmap._sourceDownloaded;
				oldValue.DownloadFailed -= bitmap._sourceFailed;
				// ((BitmapSource)newValue).DecodeFailed -= bitmap._sourceFailed; // 3.5
			}
			if ((newValue != null) && !newValue.IsFrozen)
			{
				newValue.DownloadCompleted += bitmap._sourceDownloaded;
				newValue.DownloadFailed += bitmap._sourceFailed;
				// ((BitmapSource)newValue).DecodeFailed += bitmap._sourceFailed; // 3.5
			}
		}

		private void OnSourceDownloaded(object sender, EventArgs e)
		{
			InvalidateMeasure();
			InvalidateVisual();
		}

		private void OnSourceFailed(object sender, ExceptionEventArgs e)
		{
			Source = null; // setting a local value seems scetchy...

			BitmapFailed?.Invoke(this, e);
		}

		private void OnLayoutUpdated(object sender, EventArgs e)
		{
			// Avoid getting into an endless loop
			if (ActualHeight == 0 || ActualWidth == 0)
				return;

			// This event just means that layout happened somewhere.  However, this is
			// what we need since layout anywhere could affect our pixel positioning.
			var pixelOffset = GetPixelOffset();

			if (!AreClose(pixelOffset, _pixelOffset))
				InvalidateVisual();
		}

		// Gets the matrix that will convert a point from "above" the
		// coordinate space of a visual into the the coordinate space
		// "below" the visual.
		private static Matrix GetVisualTransform(Visual v)
		{
			if (v != null)
			{
				var m = Matrix.Identity;

				var transform = VisualTreeHelper.GetTransform(v);
				if (transform != null)
				{
					var cm = transform.Value;
					m = Matrix.Multiply(m, cm);
				}

				var offset = VisualTreeHelper.GetOffset(v);
				m.Translate(offset.X, offset.Y);

				return m;
			}

			return Matrix.Identity;
		}

		private static Point TryApplyVisualTransform(Point point, Visual v, bool inverse, bool throwOnError, out bool success)
		{
			success = true;
			if (v != null)
			{
				var visualTransform = GetVisualTransform(v);
				if (inverse)
				{
					if (!throwOnError && !visualTransform.HasInverse)
					{
						success = false;
						return new Point(0, 0);
					}
					visualTransform.Invert();
				}
				point = visualTransform.Transform(point);
			}
			return point;
		}

		private static Point ApplyVisualTransform(Point point, Visual v, bool inverse)
		{
			bool success;
			return TryApplyVisualTransform(point, v, inverse, true, out success);
		}

		private Point GetPixelOffset()
		{
			var pixelOffset = new Point();

			var ps = PresentationSource.FromVisual(this);
			if (ps != null && ps.CompositionTarget != null)
			{
				var rootVisual = ps.RootVisual;

				// Transform (0,0) from this element up to pixels.
				pixelOffset = TransformToAncestor(rootVisual).Transform(pixelOffset);
				pixelOffset = ApplyVisualTransform(pixelOffset, rootVisual, false);
				pixelOffset = ps.CompositionTarget.TransformToDevice.Transform(pixelOffset);

				// Round the origin to the nearest whole pixel.
				pixelOffset.X = Math.Round(pixelOffset.X);
				pixelOffset.Y = Math.Round(pixelOffset.Y);

				// Transform the whole-pixel back to this element.
				pixelOffset = ps.CompositionTarget.TransformFromDevice.Transform(pixelOffset);
				pixelOffset = ApplyVisualTransform(pixelOffset, rootVisual, true);
				pixelOffset = rootVisual.TransformToDescendant(this).Transform(pixelOffset);
			}

			return pixelOffset;
		}

		private static bool AreClose(Point point1, Point point2)
		{
			return AreClose(point1.X, point2.X) && AreClose(point1.Y, point2.Y);
		}

		private static bool AreClose(double value1, double value2)
		{
			if (value1 == value2)
				return true;

			var delta = value1 - value2;
			return ((delta < 1.53E-06) && (delta > -1.53E-06));
		}
	}
}