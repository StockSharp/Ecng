namespace Ecng.Tests.Drawing;

#if NET10_0
using System.Drawing;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Drawing;

internal sealed class ImageSharpCodecReference : IDisposable
{
	private readonly Assembly _assembly;
	private readonly string _directory;
	private readonly Type _imageType;
	private readonly Type _contextType;
	private readonly object _configuration;
	private readonly int _oldDegree;
	private readonly Func<Stream, object> _load;
	private readonly Func<Stream, object> _identify;
	private readonly Action<object, byte[]> _copyPixels;
	private readonly Action<object, Stream, object> _save;
	private readonly List<IDisposable> _ownedImages = [];

	public string Version => _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? _assembly.GetName().Version.ToString();

	public ImageSharpCodecReference(TestContext context)
	{
		_directory = context.Properties["ImageSharpReferencePath"] as string;
		if (string.IsNullOrEmpty(_directory))
			throw new InvalidOperationException("Set ImageSharpReferencePath to a directory containing SixLabors.ImageSharp.dll.");

		AssemblyLoadContext.Default.Resolving += Resolve;
		try
		{
			_assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(_directory), "SixLabors.ImageSharp.dll"));
			_configuration = Type("SixLabors.ImageSharp.Configuration").GetProperty("Default").GetValue(null);
			_oldDegree = (int)_configuration.GetType().GetProperty("MaxDegreeOfParallelism").GetValue(_configuration);
			Set(_configuration, "MaxDegreeOfParallelism", 1);
			_imageType = Type("SixLabors.ImageSharp.Image");
			_contextType = Type("SixLabors.ImageSharp.Processing.IImageProcessingContext");
			var rgba = Type("SixLabors.ImageSharp.PixelFormats.Rgba32");
			var typedImage = Type("SixLabors.ImageSharp.Image`1").MakeGenericType(rgba);
			var load = _imageType.GetMethods().Single(m => m.Name == "Load" && m.IsGenericMethodDefinition
				&& m.GetParameters() is var p && p.Length == 1 && p[0].ParameterType == typeof(Stream)).MakeGenericMethod(rgba);
			var stream = Expression.Parameter(typeof(Stream));
			_load = Expression.Lambda<Func<Stream, object>>(Expression.Convert(Expression.Call(load, stream), typeof(object)), stream).Compile();
			_identify = Expression.Lambda<Func<Stream, object>>(Expression.Convert(Expression.Call(
				_imageType.GetMethod("Identify", [typeof(Stream)]), stream), typeof(object)), stream).Compile();

			var image = Expression.Parameter(typeof(object));
			var bytes = Expression.Parameter(typeof(byte[]));
			var span = Expression.New(typeof(Span<byte>).GetConstructor([typeof(byte[])]), bytes);
			_copyPixels = Expression.Lambda<Action<object, byte[]>>(Expression.Call(Expression.Convert(image, typedImage),
				typedImage.GetMethod("CopyPixelDataTo", [typeof(Span<byte>)]), span), image, bytes).Compile();

			var encoderType = Type("SixLabors.ImageSharp.Formats.IImageEncoder");
			var encoder = Expression.Parameter(typeof(object));
			var save = _imageType.GetMethod("Save", [typeof(Stream), encoderType]);
			_save = Expression.Lambda<Action<object, Stream, object>>(Expression.Call(Expression.Convert(image, _imageType),
				save ?? throw new MissingMethodException(_imageType.FullName, "Save"), stream, Expression.Convert(encoder, encoderType)), image, stream, encoder).Compile();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public byte[] Decode(byte[] data, out Size size)
	{
		using var stream = new MemoryStream(data, false);
		var image = _load(stream);
		using var owner = (IDisposable)image;
		size = new((int)_imageType.GetProperty("Width").GetValue(image), (int)_imageType.GetProperty("Height").GetValue(image));
		var pixels = new byte[checked(size.Width * size.Height * 4)];
		_copyPixels(image, pixels);
		return pixels;
	}

	public Size GetSize(byte[] data)
	{
		using var stream = new MemoryStream(data, false);
		var info = _identify(stream);
		return new((int)info.GetType().GetProperty("Width").GetValue(info), (int)info.GetType().GetProperty("Height").GetValue(info));
	}

	public IReadOnlyList<byte[]> DecodeFrames(byte[] data)
	{
		using var stream = new MemoryStream(data, false);
		var image = _load(stream);
		using var owner = (IDisposable)image;
		var frames = image.GetType().GetProperty("Frames", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(image);
		var count = (int)frames.GetType().GetProperty("Count").GetValue(frames);
		var clone = frames.GetType().GetMethod("CloneFrame", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, [typeof(int)]);
		var size = (int)_imageType.GetProperty("Width").GetValue(image) * (int)_imageType.GetProperty("Height").GetValue(image) * 4;
		var result = new List<byte[]>(count);
		for (var index = 0; index < count; index++)
		{
			var frame = clone.Invoke(frames, [index]);
			using var frameOwner = (IDisposable)frame;
			var pixels = new byte[size];
			_copyPixels(frame, pixels);
			result.Add(pixels);
		}
		return result;
	}

	public int GetFrameCount(byte[] data)
	{
		using var stream = new MemoryStream(data, false);
		var info = _identify(stream);
		var frames = (IEnumerable)info.GetType().GetProperty("FrameMetadataCollection").GetValue(info);
		return frames.Cast<object>().Count();
	}

	public object Encoder(ImageFormats format, int quality = 90, bool isLossless = true)
	{
		var encoder = Activator.CreateInstance(Type("SixLabors.ImageSharp.Formats." + (format switch
		{
			ImageFormats.Png => "Png.PngEncoder",
			ImageFormats.Jpeg => "Jpeg.JpegEncoder",
			ImageFormats.Bmp => "Bmp.BmpEncoder",
			ImageFormats.Gif => "Gif.GifEncoder",
			ImageFormats.Webp => "Webp.WebpEncoder",
			_ => throw new ArgumentOutOfRangeException(nameof(format)),
		})));
		Set(encoder, "SkipMetadata", true);
		switch (format)
		{
			case ImageFormats.Png:
				Set(encoder, "ColorType", "RgbWithAlpha");
				Set(encoder, "BitDepth", "Bit8");
				Set(encoder, "FilterMethod", "Adaptive");
				Set(encoder, "CompressionLevel", 6);
				break;
			case ImageFormats.Jpeg:
				Set(encoder, "Quality", quality);
				Set(encoder, "ColorType", "YCbCrRatio444");
				break;
			case ImageFormats.Webp:
				Set(encoder, "FileFormat", isLossless ? "Lossless" : "Lossy");
				Set(encoder, "Quality", quality);
				Set(encoder, "TransparentColorMode", "Preserve");
				break;
			case ImageFormats.Bmp:
				Set(encoder, "BitsPerPixel", 32);
				Set(encoder, "SupportTransparency", true);
				break;
		}
		return encoder;
	}

	public Func<byte[]> PreparedEncoder(byte[] source, object encoder)
	{
		using var stream = new MemoryStream(source, false);
		var image = _load(stream);
		_ownedImages.Add((IDisposable)image);
		return () =>
		{
			using var output = new MemoryStream();
			_save(image, output, encoder);
			return output.ToArray();
		};
	}

	public Func<byte[], byte[]> Processor(object encoder, int width = 0, int height = 0)
	{
		Action<object> resize = null;
		if (width > 0)
		{
			var options = Activator.CreateInstance(Type("SixLabors.ImageSharp.Processing.ResizeOptions"));
			Set(options, "Mode", "Stretch");
			Set(options, "Size", Activator.CreateInstance(Type("SixLabors.ImageSharp.Size"), width, height));
			Set(options, "Sampler", Type("SixLabors.ImageSharp.Processing.KnownResamplers").GetProperty("Box").GetValue(null));
			Set(options, "Compand", false);
			Set(options, "PremultiplyAlpha", true);
			var method = Type("SixLabors.ImageSharp.Processing.ResizeExtensions").GetMethods().Single(m => m.Name == "Resize"
				&& m.GetParameters() is var p && p.Length == 2 && p[0].ParameterType == _contextType && p[1].ParameterType == options.GetType());
			var parameter = Expression.Parameter(_contextType);
			var actionType = typeof(Action<>).MakeGenericType(_contextType);
			var action = Expression.Lambda(actionType, Expression.Block(Expression.Call(method, parameter, Expression.Constant(options)), Expression.Empty()), parameter).Compile();
			var mutate = Type("SixLabors.ImageSharp.Processing.ProcessingExtensions").GetMethods().Single(m => m.Name == "Mutate" && !m.IsGenericMethod
				&& m.GetParameters() is var p && p.Length == 2 && p[0].ParameterType == _imageType && p[1].ParameterType == actionType);
			var image = Expression.Parameter(typeof(object));
			resize = Expression.Lambda<Action<object>>(Expression.Call(mutate, Expression.Convert(image, _imageType), Expression.Constant(action, actionType)), image).Compile();
		}

		return data =>
		{
			using var input = new MemoryStream(data, false);
			var image = _load(input);
			using var owner = (IDisposable)image;
			resize?.Invoke(image);
			using var output = new MemoryStream();
			_save(image, output, encoder);
			return output.ToArray();
		};
	}

	private Type Type(string name) => _assembly.GetType(name, throwOnError: true);

	private static void Set(object target, string name, object value)
	{
		var property = target.GetType().GetProperty(name) ?? throw new MissingMemberException(target.GetType().FullName, name);
		var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
		if (type.IsEnum)
			value = value is string text ? Enum.Parse(type, text) : Enum.ToObject(type, value);
		property.SetValue(target, value);
	}

	private Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
	{
		var file = Path.Combine(_directory, name.Name + ".dll");
		return File.Exists(file) ? context.LoadFromAssemblyPath(file) : null;
	}

	public void Dispose()
	{
		foreach (var image in _ownedImages) image.Dispose();
		_ownedImages.Clear();
		AssemblyLoadContext.Default.Resolving -= Resolve;
		if (_configuration is not null) Set(_configuration, "MaxDegreeOfParallelism", _oldDegree);
	}
}
#endif
