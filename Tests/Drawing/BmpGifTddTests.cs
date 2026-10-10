namespace Ecng.Tests.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Ecng.Drawing;

/// <summary>
/// Independent Pillow-created BMP/GIF fixtures and byte-level APNG/GIF assertions.
/// TDD: tests committed before any GIF/BMP production decoder.
/// </summary>
[TestClass]
public class BmpGifTddTests : BaseTestClass
{
	private static readonly byte[] _rgbBmp = Convert.FromBase64String("Qk1OAAAAAAAAADYAAAAoAAAAAwAAAAIAAAABABgAAAAAABgAAADEDgAAxA4AAAAAAAAAAAAAAP////8A/wD/AAAAAAD/AP8A/wAAAAAA");
	private static readonly byte[] _rgbaBmp = Convert.FromBase64String("Qk1OAAAAAAAAADYAAAAoAAAAAwAAAAIAAAABACAAAAAAABgAAADEDgAAxA4AAAAAAAAAAAAAAP//ZP//AAD/AP/cAAD//wD/AP//AAD/");
	// GIF89a, four independently encoded indexed frames, durations 50/120/200/70ms,
	// loop=2, disposal modes 2/3/2/1. Generated using Pillow, not Ecng codec.
	private static readonly byte[] _animated = Convert.FromBase64String(
		"R0lGODlhBAAEAIEAAAAAAP8AAAAAAAAAACH/C05FVFNDQVBFMi4wAwECAAAh+QQJBQAAACwAAAAABAAEAAAIDQADABgokOBAAAUBBAQAIfkEDQwAAAAsAQAAAAEABACBAAAAAP8AAAAAAAAACAYAAwgMEBAAIfkECRQAAAAsAQAAAAIABACBAAAAAAD/AAAAAAAACAgAAQQQSDBAQAAh+QQFBwAAACwDAAAAAQAEAIEAAAD//wAAAAAAAAAIBgADCAwQEAA7");
	private static readonly byte[] _watermarkGif = Convert.FromBase64String(
		"R0lGODlhQAAwAIEAAAAAAP8AAAAAAAAAACH/C05FVFNDQVBFMi4wAwEAAAAh+QQJBAAAACwAAAAAQAAwAAAI/wADCBwoEIDBgwgTAiBIUKHDgwwHPoxY8KFDigEsXqQ4kaPGhBg/gvSoMKRIiCRPmiyZUuRKlS1RRjwpkyFNgy8R5vy4U2PPhTF9BrX48ybQmTeLGt2IlKbSpSObwpQKVShVnkOrRrWZNKvWmg27Xv26NaxTr2SflpVI9ijXs2OJDlWrE+3aiizjdtTL9O3djGL9upxrty7fvILBsoWb2Gpjt2anPt77mK5ivJIxRh58+C/lzVg7G65c+DJgxqBxllYt2vTnxZlhcybdmvXkvqkhyw59G3Fuy7Zzv8asFbhu4m1HC8e9O3nw5nJrLzVu3Cj11dOxVw+83Df05NelDyU/HRu549/a08cN39szc/PR2yuHzr67e+/wx1snLL0+ffXymRYQACH5BA0HAAAALAAAAABAADAAgQAAAAD/AAAAAAAAAAj/AAEIHEiwIIAACBMiNMhwoEKFDSNKLPgw4USKFQNc3Mgwo0aOBzOCHBmy4kiPJEGiVCky5caVHGG6lCjzYs2ZHVvG1InzJk2eNoE6FPrTJEujBn1GVNpQKdOcSF8SLfnw5NSmU58mvQq1aleIR73ujEpQK0ayEz2aLct1K1qBa4e+XZq17VmxUufGhWuXrd6+cvEG/TsXa+GvFhEvDAt2rODAjfM+LjqZ5F6qkXEq/ug4s+a7nimH/sz3sNvKpC9ftmoadGLSp0cbRv1ZNWDRryXL3jx7d+zcgyvbbu1X+G3MwNPWJQ45Oe7Fv6Hrdk6XMO3onKdLL+7b9Xblaq97Ks8efPRw8dypn++e/ntp9M3dVzfO/D37+OTxs4ZvX/3x9f7Vh5x8vVEXEAAh+QQFCQAAACwAAAAAQAAwAIEAAAAAAP8AAAAAAAAI/wABCBwoMIDBgwYJKlwIACFChhAbOkzIcCLFiAotBsC4UCNEjxwJggxZ0OJHkyRLTkyp0uHJlSxHkpQpEmVKmhxxtnwY0+ZMnzVh3gSaUyNRjDojJk360mVPoRmPKpXalGdHqhWxXoUa1OlQrkXB7jzIUqLYqWeZZj1blexarz/ZvrVaNqrcrXDrhs27l65epFrt8v071+3XwYQF++1rOHHbi3EROx4LOaRax5cVNyacuetiwGkDe97MuPLAzqdFpw59V7Np0IhRU974NLbq2WWX3jYrufBr3p/R9sZrlDTs4I9pEzcuHLlv5a6hW96tu/Xo38mz13a+/Lds4My1dyqXXpr8cebf0+8WH5l7YvXW/8If/p76+u3hn09unn/8fvbTxafXfO4RFBAAOw==");

	[TestMethod]
	public void Bmp_IndependentPillowRgb24_DimensionsAndEveryPixel()
	{
		_rgbBmp.GetImageSize().Width.AssertEqual(3);
		_rgbBmp.GetImageSize().Height.AssertEqual(2);
		var png = _rgbBmp.ConvertToPng();
		var expected = new byte[]
		{
			255,0,0,255,0,255,0,255,0,0,255,255,
			255,255,0,255,0,255,255,255,255,0,255,255
		};
		Compare(expected, Pixels(png,3,2));
	}

	[TestMethod]
	public void Bmp_PillowRgb24_ResizePreservesFormat_AndNoUpscaleIsByteIdentical()
	{
		var source=(byte[])_rgbBmp.Clone();
		var same=source.ResizeImage(100,100);
		Compare(source,same);
		var resized=source.ResizeImage(2,2);
		(resized[0]=='B' && resized[1]=='M').AssertTrue();
		resized.GetImageSize().Width.AssertEqual(2);
		resized.GetImageSize().Height.AssertEqual(1);
		Compare(source,_rgbBmp);
	}

	[TestMethod]
	public void Bmp_Watermark_ProducesBmpAndDoesNotAlterSource()
	{
		var source=(byte[])_rgbaBmp.Clone();
		var marked=source.AddTextWatermark("hello",opacity:0,margin:0);
		(marked[0]=='B' && marked[1]=='M').AssertTrue();
		Compare(Pixels(source.ConvertToPng(),3,2),Pixels(marked.ConvertToPng(),3,2));
		Compare(source,_rgbaBmp);
	}

	[TestMethod]
	public void Gif_PillowFourFrameFixture_InspectEveryCompositedFrameAndTiming()
	{
		_animated.GetImageSize().Width.AssertEqual(4);
		_animated.GetImageSize().Height.AssertEqual(4);
		var frames = _animated.GetGifFrames();
		frames.Count.AssertEqual(4);
		var times = new[] { 50,120,200,70 };
		byte[][] colors =
		[
			[255,0,0,255], [0,255,0,255],
			[0,0,255,255], [255,255,0,255]
		];

		for (var frame=0;frame<4;frame++)
		{
			frames[frame].delayMilliseconds.AssertEqual(times[frame]);
			var rgba=Pixels(frames[frame].png,4,4);
			for(var y=0;y<4;y++)
				for(var x=0;x<4;x++)
				{
					var offset=(y*4+x)*4;
					var wanted = x==frame ? colors[frame] : new byte[]{0,0,0,0};
					for(var c=0;c<4;c++)
						rgba[offset+c].AssertEqual(wanted[c]);
				}
		}
		_animated.GetGifLoopCount().AssertEqual(2);
	}

	[TestMethod]
	public void Gif_Resize_AllFourFramesStayAnimated_PreservesDelayAndLoops()
	{
		var output=_animated.ResizeImage(2,2);
		(output[0]=='G' && output[1]=='I' && output[2]=='F').AssertTrue();
		output.GetImageSize().Width.AssertEqual(2);
		output.GetImageSize().Height.AssertEqual(2);
		output.GetGifLoopCount().AssertEqual(2);
		var frames=output.GetGifFrames();
		frames.Count.AssertEqual(4);
		var delays=new[]{50,120,200,70};
		for(var i=0;i<4;i++)
		{
			frames[i].delayMilliseconds.AssertEqual(delays[i]);
			frames[i].png.GetImageSize().Width.AssertEqual(2);
			frames[i].png.GetImageSize().Height.AssertEqual(2);
		}
	}

	[TestMethod]
	public void Gif_ConvertToPng_PreservesEveryFrameAsApng()
	{
		var apng=_animated.ConvertToPng();
		apng.GetImageSize().Width.AssertEqual(4);
		apng.GetImageSize().Height.AssertEqual(4);
		var chunks=ReadPngChunks(apng);
		var animation=chunks.Single(x=>x.tag=="acTL").data;
		BinaryPrimitives.ReadUInt32BigEndian(animation.AsSpan(0,4)).AssertEqual((uint)4);
		BinaryPrimitives.ReadUInt32BigEndian(animation.AsSpan(4,4)).AssertEqual((uint)2);
		chunks.Count(x=>x.tag=="fcTL").AssertEqual(4);
		chunks.Count(x=>x.tag=="fdAT").AssertEqual(3);
		Compare(Pixels(_animated.GetGifFrames()[0].png,4,4),Pixels(apng,4,4));
	}



	[TestMethod]
	public void Apng_OutputFromGif_ResizeMustPreserveAllFourFramesAndDelays()
	{
		var apng = _animated.ConvertToPng();
		var resized = apng.ResizeImage(2, 2);
		var chunks = ReadPngChunks(resized);
		var acTl = chunks.Single(x=>x.tag=="acTL").data;
		BinaryPrimitives.ReadUInt32BigEndian(acTl.AsSpan(0,4)).AssertEqual((uint)4);
		BinaryPrimitives.ReadUInt32BigEndian(acTl.AsSpan(4,4)).AssertEqual((uint)2);
		chunks.Count(x=>x.tag=="fcTL").AssertEqual(4);
		chunks.Count(x=>x.tag=="fdAT").AssertEqual(3);
		resized.GetImageSize().Width.AssertEqual(2);
		resized.GetImageSize().Height.AssertEqual(2);
		var delays = new[]{5,12,20,7};
		var controls = chunks.Where(x=>x.tag=="fcTL").ToArray();
		for (var i=0;i<4;i++)
			BinaryPrimitives.ReadUInt16BigEndian(controls[i].data.AsSpan(20,2))
				.AssertEqual((ushort)delays[i]);
	}

	[TestMethod]
	public void Apng_OutputFromGif_InvisibleWatermarkMustNotDiscardAnyFrame()
	{
		var apng = _animated.ConvertToPng();
		var output = apng.AddTextWatermark("Invisible",opacity:0,margin:0);
		var before = ReadPngChunks(apng);
		var after = ReadPngChunks(output);
		before.Count(x=>x.tag=="fcTL").AssertEqual(after.Count(x=>x.tag=="fcTL"));
		after.Count(x=>x.tag=="fcTL").AssertEqual(4);
		after.Count(x=>x.tag=="fdAT").AssertEqual(3);
	}

	[TestMethod]
	public void Gif_ApngExport_CheckEveryFramePayloadAndControlAgainstIndependentColors()
	{
		var apng = _animated.ConvertToPng();
		var chunks = ReadPngChunks(apng);
		var expectedDurations = new[] { 5,12,20,7 }; // centiseconds
		byte[][] colors =
		[
			[255,0,0,255], [0,255,0,255],
			[0,0,255,255], [255,255,0,255]
		];

		var seq = 0u;
		var frameIndex = -1;
		using var currentCompressed = new MemoryStream();

		void VerifyCompleted()
		{
			if (frameIndex < 0) return;
			currentCompressed.Position = 0;
			using var decoded = new MemoryStream();
			using(var zlib = new ZLibStream(currentCompressed, CompressionMode.Decompress, leaveOpen:true))
				zlib.CopyTo(decoded);
			var pixels = decoded.ToArray();
			pixels.Length.AssertEqual(4*(1+4*4));
			for(var y=0;y<4;y++)
			{
				pixels[y*17].AssertEqual((byte)0);
				for(var x=0;x<4;x++)
					for(var c=0;c<4;c++)
					{
						var expected = x==frameIndex ? colors[frameIndex][c] : (byte)0;
						pixels[y*17+1+x*4+c].AssertEqual(expected);
					}
			}
			currentCompressed.SetLength(0);
		}

		foreach (var chunk in chunks)
		{
			if (chunk.tag == "fcTL")
			{
				VerifyCompleted();
				frameIndex++;
				var control = chunk.data;
				control.Length.AssertEqual(26);
				BinaryPrimitives.ReadUInt32BigEndian(control.AsSpan(0,4)).AssertEqual(seq++);
				BinaryPrimitives.ReadUInt32BigEndian(control.AsSpan(4,4)).AssertEqual((uint)4);
				BinaryPrimitives.ReadUInt32BigEndian(control.AsSpan(8,4)).AssertEqual((uint)4);
				BinaryPrimitives.ReadUInt16BigEndian(control.AsSpan(20,2)).AssertEqual(
					(ushort)expectedDurations[frameIndex]);
				BinaryPrimitives.ReadUInt16BigEndian(control.AsSpan(22,2)).AssertEqual((ushort)100);
				control[24].AssertEqual((byte)0);
				control[25].AssertEqual((byte)0);
			}
			else if (chunk.tag == "IDAT")
				currentCompressed.Write(chunk.data);
			else if (chunk.tag == "fdAT")
			{
				BinaryPrimitives.ReadUInt32BigEndian(chunk.data.AsSpan(0,4)).AssertEqual(seq++);
				currentCompressed.Write(chunk.data,4,chunk.data.Length-4);
			}
		}
		VerifyCompleted();
		(frameIndex+1).AssertEqual(4);
	}

	[TestMethod]
	public void Gif_Watermark_EachOfThreeFramesChanged_AnimationPreserved()
	{
		var file=Path.Combine(Path.GetTempPath(),$"ecng-gif-{Guid.NewGuid():N}.ttf");
		File.WriteAllBytes(file,ImagePixelGoldenTests.BuildTestTrueType());
		try
		{
			var source=(byte[])_watermarkGif.Clone();
			var expected=source.GetGifFrames();
			var result=source.AddTextWatermark("I",fontSize:18,opacity:255,
				margin:8,fontFilePath:file);
			(result[0]=='G' && result[1]=='I' && result[2]=='F').AssertTrue();
			result.GetGifLoopCount().AssertEqual(0);
			var frames=result.GetGifFrames();
			frames.Count.AssertEqual(3);
			for(var i=0;i<frames.Count;i++)
			{
				frames[i].delayMilliseconds.AssertEqual(expected[i].delayMilliseconds);
				CompareNonEqual(Pixels(expected[i].png,64,48),Pixels(frames[i].png,64,48));
			}
			Compare(source,_watermarkGif);
		}
		finally { File.Delete(file); }
	}

	[TestMethod]
	public void Gif_ZeroOpacityWatermark_LeavesAllFramesVisuallyUnchanged()
	{
		var source=_animated.GetGifFrames();
		var output=_animated.AddTextWatermark("Invisible",fontSize:4,opacity:0,margin:0);
		var frames=output.GetGifFrames();
		frames.Count.AssertEqual(source.Count);
		for(var i=0;i<source.Count;i++)
		{
			frames[i].delayMilliseconds.AssertEqual(source[i].delayMilliseconds);
			Compare(Pixels(source[i].png,4,4),Pixels(frames[i].png,4,4));
		}
	}

	[TestMethod]
	public void Gif_TruncatedAndCorruptFiles_FailInControlledManner()
	{
		foreach(var length in new[]{1,6,13,25,_animated.Length/2,_animated.Length-1})
		{
			try
			{
				_animated[..length].ConvertToPng();
				Assert.Fail($"Truncated GIF of {length} bytes was accepted.");
			}
			catch(InvalidDataException) {}
			catch(NotSupportedException) {} // <6-byte data is unrecognizable as GIF.
		}

		var random=new Random(18057);
		for(var iter=0;iter<100;iter++)
		{
			var bytes=(byte[])_animated.Clone();
			var index=random.Next(13,bytes.Length-1);
			bytes[index]^=(byte)(1<<random.Next(8));
			try
			{
				var frames=bytes.GetGifFrames();
				(frames.Count > 0 && frames.Count <= 512).AssertTrue();
			}
			catch(InvalidDataException) {}
			catch(NotSupportedException) {}
		}
	}


	[TestMethod]
	public void Apng_ReimportEveryGifFrame_AgreesWithIndependentPillowPixelOracle()
	{
		var animation=_animated.ConvertToPng();
		var frames=animation.GetAnimationFrames();
		frames.Count.AssertEqual(4);
		animation.GetAnimationLoopCount().AssertEqual(2);
		var original=_animated.GetGifFrames();
		for(var i=0;i<4;i++)
		{
			frames[i].delayMilliseconds.AssertEqual(original[i].delayMilliseconds);
			Compare(Pixels(original[i].png,4,4),Pixels(frames[i].png,4,4));
		}
	}

	[TestMethod]
	public void Apng_FullFrameBlendSourceAndBlendOver_RespectTransparentPixels()
	{
		var source=ReadPngChunks(_animated.ConvertToPng());
		var controls=0;
		foreach(var i in Enumerable.Range(0,source.Length))
			if(source[i].tag=="fcTL")
			{
				// First frame retains red column; second frame should composite
				// green over red instead of wiping transparent pixels.
				if(controls==1)source[i].data[25]=1;
				controls++;
			}
		var apng=BuildApngChunks(source);
		var frames=apng.GetAnimationFrames();
		var second=Pixels(frames[1].png,4,4);
		for(var y=0;y<4;y++)
		{
			Compare([255,0,0,255],second.AsSpan((y*4+0)*4,4).ToArray());
			Compare([0,255,0,255],second.AsSpan((y*4+1)*4,4).ToArray());
			Compare([0,0,0,0],second.AsSpan((y*4+2)*4,4).ToArray());
		}
	}

	[TestMethod]
	public void Apng_AllThreeDisposalModes_RenderExpectedFramePixels()
	{
		for(var disposal=0;disposal<3;disposal++)
		{
			var chunks=ReadPngChunks(_animated.ConvertToPng());
			var first=Array.FindIndex(chunks,c=>c.tag=="fcTL");
			chunks[first].data[24]=(byte)disposal;
			var second=Array.FindIndex(chunks,first+1,c=>c.tag=="fcTL");
			chunks[second].data[25]=1; // overlay on previous canvas
			var frames=BuildApngChunks(chunks).GetAnimationFrames();
			var next=Pixels(frames[1].png,4,4);
			// NONE preserves red; BACKGROUND and PREVIOUS clear/restore it.
			for(var y=0;y<4;y++)
				next[(y*4)*4+3].AssertEqual((byte)(disposal==0?255:0));
		}
	}

	[TestMethod]
	public void Apng_MalformedFrameSequenceDimensionsCrcOrMetadata_FailsClosed()
	{
		var bytes=_animated.ConvertToPng();
		var changed=(byte[])bytes.Clone();
		changed[changed.Length-24]^=0x80;
		ThrowsExactly<InvalidDataException>(()=>changed.GetAnimationFrames());

		void Reject(Action<(string tag,byte[] data)[]> change)
		{
			var chunks=ReadPngChunks(bytes);
			change(chunks);
			ThrowsExactly<InvalidDataException>(()=>BuildApngChunks(chunks).GetAnimationFrames());
		}
		Reject(chunks=>BinaryPrimitives.WriteUInt32BigEndian(chunks.Single(c=>c.tag=="acTL").data.AsSpan(0,4),8));
		Reject(chunks=>BinaryPrimitives.WriteUInt32BigEndian(chunks.First(c=>c.tag=="fcTL").data.AsSpan(0,4),100));
		Reject(chunks=>BinaryPrimitives.WriteUInt32BigEndian(chunks.First(c=>c.tag=="fdAT").data.AsSpan(0,4),100));
		Reject(chunks=>BinaryPrimitives.WriteUInt32BigEndian(chunks.First(c=>c.tag=="fcTL").data.AsSpan(4,4),5));
		Reject(chunks=>chunks.First(c=>c.tag=="fcTL").data[24]=3);
		Reject(chunks=>chunks.First(c=>c.tag=="fcTL").data[25]=2);
		Reject(chunks=> { var at=Array.FindIndex(chunks,c=>c.tag=="fdAT"); chunks[at] = (chunks[at].tag, Array.Empty<byte>()); });
	}

	private static byte[] BuildApngChunks((string tag,byte[] data)[] chunks)
	{
		using var output=new MemoryStream();
		output.Write([137,80,78,71,13,10,26,10]);
		foreach(var(tag,payload) in chunks)
		{
			Span<byte> number=stackalloc byte[4];
			BinaryPrimitives.WriteUInt32BigEndian(number,(uint)payload.Length);
			output.Write(number);
			var name=Encoding.ASCII.GetBytes(tag);
			output.Write(name);output.Write(payload);
			uint crc=0xFFFFFFFF;
			foreach(var b in name.Concat(payload))
			{
				crc^=b;
				for(var i=0;i<8;i++)crc=(crc&1)!=0?(crc>>1)^0xEDB88320:crc>>1;
			}
			BinaryPrimitives.WriteUInt32BigEndian(number,~crc);
			output.Write(number);
		}
		return output.ToArray();
	}

	private static void Compare(byte[] expected,byte[] actual)
	{
		expected.Length.AssertEqual(actual.Length);
		for(var i=0;i<expected.Length;i++)
			if(expected[i]!=actual[i])
				Assert.Fail($"Byte {i}: expected {expected[i]} got {actual[i]}");
	}

	private static void CompareNonEqual(byte[] expected,byte[] actual)
	{
		expected.Length.AssertEqual(actual.Length);
		if(expected.SequenceEqual(actual))
			Assert.Fail("Expected watermark to change the frame pixels.");
	}

	private static byte[] Pixels(byte[] png,int width,int height)
	{
		using var idat=new MemoryStream();
		foreach(var c in ReadPngChunks(png))
			if(c.tag=="IDAT")idat.Write(c.data);
		idat.Position=0;
		using var decoded=new MemoryStream();
		using(var z=new ZLibStream(idat,CompressionMode.Decompress))
			z.CopyTo(decoded);
		var src=decoded.ToArray();
		if(src.Length!=height*(1+width*4))Assert.Fail("PNG test output is not RGBA8/filter-0.");
		var pixels=new byte[width*height*4];
		for(var y=0;y<height;y++)
		{
			src[y*(1+width*4)].AssertEqual((byte)0);
			Array.Copy(src,y*(1+width*4)+1,pixels,y*width*4,width*4);
		}
		return pixels;
	}

	private static (string tag,byte[] data)[] ReadPngChunks(byte[] png)
	{
		var list=new System.Collections.Generic.List<(string tag,byte[] data)>();
		for(var pos=8;pos+12<=png.Length;)
		{
			var length=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos,4));
			if(length<0||length>png.Length-pos-12)
				throw new InvalidDataException("PNG fixture chunk bounds.");
			var tag=Encoding.ASCII.GetString(png,pos+4,4);
			list.Add((tag,png.AsSpan(pos+8,length).ToArray()));
			pos+=length+12;
		}
		return list.ToArray();
	}
}
