namespace Ecng.Tests.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Ecng.Drawing;

/// <summary>
/// Independent byte-level PNG encoder generates adversarial and legal fixtures.
/// Production decoder is never used to create expected pixels.
/// </summary>
[TestClass]
public class PngCodecStressTests : BaseTestClass
{
	private static readonly (int x, int y, int dx, int dy)[] _adam7 =
	[
		(0,0,8,8),(4,0,8,8),(0,4,4,8),(2,0,4,4),
		(0,2,2,4),(1,0,2,2),(0,1,1,2)
	];

	[TestMethod]
	public void Png_AllFiveFilterTypes_RestoreEveryRgbaChannelExactly()
	{
		var expected = Pattern(19, 13);
		for (var filter = 0; filter <= 4; filter++)
		{
			var png = Pack(19, 13, expected, filter, interlace: false);
			Compare(expected, DecodeOutput(png.ConvertToPng(), 19, 13), 19);
		}
	}

	[TestMethod]
	public void Png_Adam7InterlacedOddSizes_EqualExpectedAtEveryPixel()
	{
		foreach (var (w,h) in new[] { (1,1),(2,3),(5,4),(13,11),(17,15) })
		{
			var pixels = Pattern(w,h);
			var png = Pack(w,h,pixels,4,interlace:true);
			Compare(pixels,DecodeOutput(png.ConvertToPng(),w,h),w);
		}
	}

	[TestMethod]
	public void Png_MultipleScanlineFiltersInSingleImage_EveryPixelMatches()
	{
		var source = Pattern(17,19);
		var png = Pack(17,19,source, -1,interlace:false);
		Compare(source,DecodeOutput(png.ConvertToPng(),17,19),17);
	}

	[TestMethod]
	public void Png_SmallRandomizedRgbaPictures_LosslessExactPixelRoundTrips()
	{
		var rand = new Random(87234);
		for (var iteration = 0; iteration < 160; iteration++)
		{
			var w = rand.Next(1,48);
			var h = rand.Next(1,45);
			var pixels = new byte[w*h*4];
			rand.NextBytes(pixels);
			var source = Pack(w,h,pixels,iteration%5, interlace:iteration%4==0);
			var copy = (byte[])source.Clone();
			Compare(pixels,DecodeOutput(source.ConvertToPng(),w,h),w);
			source.SequenceEqual(copy).AssertTrue();
		}
	}

	[TestMethod]
	public void Png_TransparencyAndPaletteFourBitAllIndicesAreExact()
	{
		// Indexed 4-bit 4x2 with four RGBA palette entries including 0/128/255 alpha.
		byte[] indices = [0,1,2,3,3,2,1,0];
		byte[] palette = [250,0,0,0,250,0,0,0,250,220,120,40];
		byte[] alpha = [0,127,255,220];
		byte[] raw = [0,0x01,0x23,0,0x32,0x10];
		var png = RawPng(4,2,4,3,0,raw,palette,alpha);
		var expected = new byte[4*2*4];
		for(var i=0;i<indices.Length;i++)
		{
			var idx=indices[i];
			Array.Copy(palette,idx*3,expected,i*4,3);
			expected[i*4+3]=alpha[idx];
		}
		Compare(expected,DecodeOutput(png.ConvertToPng(),4,2),4);
	}

	[TestMethod]
	public void Png_Rgba16Bit_ConvertsEveryChannelToHighOrderByte()
	{
		byte[] samples = [12,34,56,78,100,120,140,160,201,217,233,249,77,33,22,11];
		var raw = new byte[2*(1+2*4*2)];
		for(var i=0;i<16;i++)
		{
			var at=1+(i/8)*17+(i%8)*2;
			raw[at]=samples[i];
			raw[at+1]=(byte)(i*11);
		}
		var png=RawPng(2,2,16,6,0,raw,[],[]);
		// Four pixels, each RGBA channel retained via high byte.
		Compare(samples,DecodeOutput(png.ConvertToPng(),2,2),2);
	}

	[TestMethod]
	public void Png_DamagedCrcAndTruncatedPayload_MustFailClosed()
	{
		var bytes = Pack(15,11,Pattern(15,11),0,false);
		for(var i=0;i<Math.Min(bytes.Length-4,150);i++)
		{
			var copy=(byte[])bytes.Clone();
			copy[i]^=0x20;
			try
			{
				copy.ConvertToPng();
				Assert.Fail($"Modified PNG byte {i} was accepted despite CRC or structural error.");
			}
			catch(InvalidDataException) { }
			catch(NotSupportedException) { }
		}
	}

	private static byte[] Pattern(int width, int height)
	{
		var data = new byte[width*height*4];
		for(var y=0;y<height;y++)
			for(var x=0;x<width;x++)
			{
				var p=(y*width+x)*4;
				data[p]=(byte)(x*17+y*11);
				data[p+1]=(byte)(x*7+y*31);
				data[p+2]=(byte)(x*53+y*3);
				data[p+3]=(byte)(x*29+y*19);
			}
		return data;
	}

	private static byte[] Pack(int w,int h,byte[] rgba,int chosenFilter,bool interlace)
	{
		using var raw=new MemoryStream();
		if(!interlace)
			WritePass(raw,rgba,w,w,h,0,0,1,1,chosenFilter);
		else
			foreach(var(x,y,dx,dy) in _adam7)
			{
				var pw=w<=x?0:(w-x+dx-1)/dx;
				var ph=h<=y?0:(h-y+dy-1)/dy;
				if(pw>0&&ph>0)
					WritePass(raw,rgba,w,pw,ph,x,y,dx,dy,chosenFilter);
			}

		return RawPng(w,h,8,6,(byte)(interlace?1:0),raw.ToArray(),[],[]);
	}

	private static void WritePass(Stream raw,byte[] rgba,int stride,int width,int height,
		int startX,int startY,int stepX,int stepY,int chosenFilter)
	{
		var prev=new byte[width*4];
		var row=new byte[width*4];
		for(var y=0;y<height;y++)
		{
			for(var x=0;x<width;x++)
				Array.Copy(rgba,((startY+y*stepY)*stride+startX+x*stepX)*4,row,x*4,4);

			var filter=chosenFilter==-1?y%5:chosenFilter;
			raw.WriteByte((byte)filter);
			for(var i=0;i<row.Length;i++)
			{
				var a=i>=4?row[i-4]:0;
				var b=prev[i];
				var c=i>=4?prev[i-4]:0;
				var prediction=filter switch
				{
					0=>0, 1=>a, 2=>b, 3=>(a+b)/2, 4=>Paeth(a,b,c),
					_=>throw new InvalidDataException()
				};
				raw.WriteByte(unchecked((byte)(row[i]-prediction)));
			}
			(row,prev)=(prev,row);
		}
	}

	private static int Paeth(int a,int b,int c)
	{
		var p=a+b-c;
		var da=Math.Abs(p-a);var db=Math.Abs(p-b);var dc=Math.Abs(p-c);
		return da<=db&&da<=dc?a:db<=dc?b:c;
	}

	private static byte[] RawPng(int w,int h,byte depth,byte color,byte interlace,
		byte[] uncompressed,byte[] palette,byte[] transparency)
	{
		using var file=new MemoryStream();
		file.Write([137,80,78,71,13,10,26,10]);
		var header=new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0,4),w);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4,4),h);
		header[8]=depth;header[9]=color;header[12]=interlace;
		Chunk(file,"IHDR",header);
		if(palette.Length!=0)Chunk(file,"PLTE",palette);
		if(transparency.Length!=0)Chunk(file,"tRNS",transparency);
		using var data=new MemoryStream();
		using(var zipper=new ZLibStream(data,CompressionLevel.Optimal,leaveOpen:true))
			zipper.Write(uncompressed);
		Chunk(file,"IDAT",data.ToArray());
		Chunk(file,"IEND",[]);
		return file.ToArray();
	}

	private static void Chunk(Stream stream,string tag,byte[] data)
	{
		Span<byte> size=stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(size,data.Length);
		stream.Write(size);
		var tagBytes=Encoding.ASCII.GetBytes(tag);
		stream.Write(tagBytes);
		stream.Write(data);
		uint crc=0xffffffff;
		foreach(var v in tagBytes.Concat(data))
		{
			crc^=v;
			for(var b=0;b<8;b++) crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1;
		}
		BinaryPrimitives.WriteUInt32BigEndian(size,~crc);
		stream.Write(size);
	}

	private static byte[] DecodeOutput(byte[] png,int w,int h)
	{
		if(png.GetPngSize().Width!=w||png.GetPngSize().Height!=h)
			Assert.Fail("Incorrect PNG output dimensions.");
		using var compressed=new MemoryStream();
		for(var p=8;p+12<=png.Length;)
		{
			var count=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(p,4));
			if(count<0||count>png.Length-p-12)Assert.Fail("Invalid PNG result chunk length.");
			if(png.AsSpan(p+4,4).SequenceEqual("IDAT"u8))
				compressed.Write(png,p+8,count);
			p+=count+12;
		}
		compressed.Position=0;
		using var inflated=new MemoryStream();
		using(var zlib=new ZLibStream(compressed,CompressionMode.Decompress))
			zlib.CopyTo(inflated);
		var rows=inflated.ToArray();
		if(rows.Length!=h*(w*4+1))Assert.Fail("Invalid PNG output row count.");
		var output=new byte[w*h*4];
		for(var y=0;y<h;y++)
		{
			if(rows[y*(w*4+1)]!=0)Assert.Fail("Unexpected PNG output filter.");
			Array.Copy(rows,y*(w*4+1)+1,output,y*w*4,w*4);
		}
		return output;
	}

	private static void Compare(byte[] expected,byte[] actual,int width)
	{
		if(expected.Length!=actual.Length)Assert.Fail($"Expected {expected.Length} bytes, actual {actual.Length}");
		for(var i=0;i<expected.Length;i++)
			if(expected[i]!=actual[i])
				Assert.Fail($"Pixel ({i/4%width},{i/4/width}), channel {"RGBA"[i%4]}: "+
					$"expected {expected[i]} actual {actual[i]}");
	}

	[TestMethod]
	public void Png_AllLegalColorTypesAndBitDepths_AgreeWithIndependentPixelOracle()
	{
		// All 15 legal (color type, bit depth) pairs, each with odd-width rows.
		foreach (var (color, depth) in new (byte color, byte depth)[]
		{
			(0,1),(0,2),(0,4),(0,8),(0,16),
			(2,8),(2,16),
			(3,1),(3,2),(3,4),(3,8),
			(4,8),(4,16),
			(6,8),(6,16)
		})
		{
			try
			{
			const int width = 5, height = 3;
			var channels = color switch { 2=>3, 4=>2, 6=>4, _=>1 };
			var maxSample = depth == 16 ? 65535 : (1 << depth) - 1;
			byte[] palette = [], transparency = [];
			if (color == 3)
			{
				var count = 1 << depth;
				palette = new byte[count*3];
				for (var index = 0; index < count; index++)
				{
					palette[3*index] = (byte)(17*index);
					palette[3*index+1] = (byte)(255-index);
					palette[3*index+2] = (byte)(index ^ 0x5a);
				}
				transparency = Enumerable.Range(0,Math.Min(count,5)).Select(i=>(byte)(i*43)).ToArray();
			}

			var samples = new int[width*height*channels];
			for (var p = 0; p < width*height; p++)
				for (var c = 0; c < channels; c++)
					samples[p*channels+c] = color == 3
						? p % (1 << depth)
						: (p*37001 + c*13111 + 17773) & maxSample;

			using var raw = new MemoryStream();
			for (var y=0; y<height; y++)
			{
				raw.WriteByte((byte)(y%5));
				// Build a zero-filtered row, then encode the appropriate PNG
				// filter separately for each byte (bpp is not always 4).
				var row = PackSamples(samples.AsSpan(y*width*channels,width*channels),depth);
				var bpp=Math.Max(1,(channels*depth+7)/8);
				var previous = y == 0 ? new byte[row.Length] :
					PackSamples(samples.AsSpan((y-1)*width*channels,width*channels),depth);
				for(var j=0;j<row.Length;j++)
				{
					var left=j>=bpp?row[j-bpp]:0;
					var up=previous[j];
					var upperLeft=j>=bpp?previous[j-bpp]:0;
					var predictor = y%5 switch
					{
						0=>0, 1=>left, 2=>up, 3=>(left+up)/2,
						4=>Paeth(left,up,upperLeft), _=>0
					};
					raw.WriteByte(unchecked((byte)(row[j]-predictor)));
				}
			}

			var expected = new byte[width*height*4];
			for(var p=0;p<width*height;p++)
			{
				int Read(int c) => samples[p*channels+c];
				byte Normal(int v) => depth==16?(byte)(v>>8):depth==8?(byte)v:
					(byte)(v*255/maxSample);
				var off=p*4;
				switch(color)
				{
					case 0:
						expected[off]=expected[off+1]=expected[off+2]=Normal(Read(0));
						expected[off+3]=255;
						break;
					case 2:
						for(var c=0;c<3;c++)expected[off+c]=Normal(Read(c));
						expected[off+3]=255;
						break;
					case 3:
						var idx=Read(0);
						Array.Copy(palette,idx*3,expected,off,3);
						expected[off+3]=idx<transparency.Length?transparency[idx]:(byte)255;
						break;
					case 4:
						expected[off]=expected[off+1]=expected[off+2]=Normal(Read(0));
						expected[off+3]=Normal(Read(1));
						break;
					case 6:
						for(var c=0;c<4;c++)expected[off+c]=Normal(Read(c));
						break;
				}
			}

			var source=RawPng(width,height,depth,color,0,raw.ToArray(),palette,transparency);
			Compare(expected,DecodeOutput(source.ConvertToPng(),width,height),width);
			}
			catch (Exception ex)
			{
				Assert.Fail($"PNG color={color}, depth={depth}: {ex}");
			}
		}
	}

	[TestMethod]
	public void Png_GrayscaleAndRgbTransparencyKeys_PreserveTransparentPixels()
	{
		foreach(var depth in new byte[]{1,2,4,8,16})
		{
			var value=depth==16?0x3040:depth==8?37:1;
			byte[] raw=[0,..PackSamples([0,value,value,depth==16?65535:(1<<depth)-1],depth)];
			byte[] key=[(byte)(value>>8),(byte)value];
			var pixels=DecodeOutput(RawPng(4,1,depth,0,0,raw,[],key).ConvertToPng(),4,1);
			for(var i=0;i<4;i++)
				pixels[4*i+3].AssertEqual((byte)(i is 1 or 2 || (i == 3 && depth == 1)?0:255));
		}

		foreach(var depth in new byte[]{8,16})
		{
			var values=depth==8?new[]{17,91,250}:new[]{0x1234,0x5678,0x9abc};
			byte[] raw=[0,..PackSamples([
				0,0,0,values[0],values[1],values[2],
				values[0],values[1],values[2],values[0],values[1],values[0]],depth)];
			var key=new byte[6];
			for(var j=0;j<3;j++)
			{
				key[2*j]=(byte)(values[j]>>8);
				key[2*j+1]=(byte)values[j];
			}
			var pixels=DecodeOutput(RawPng(4,1,depth,2,0,raw,[],key).ConvertToPng(),4,1);
			for(var i=0;i<4;i++)
				pixels[4*i+3].AssertEqual((byte)(i is 1 or 2?0:255));
		}
	}

	[TestMethod]
	public void Png_UnknownCriticalChunkRejected_AncillaryChunkIgnored()
	{
		var source=Pack(2,2,Pattern(2,2),0,false);
		byte[] WithChunk(string tag)
		{
			using var output=new MemoryStream();
			output.Write(source,0,33); // signature + IHDR
			Chunk(output,tag,[11,22,33]);
			output.Write(source,33,source.Length-33);
			return output.ToArray();
		}
		ThrowsExactly<NotSupportedException>(()=>WithChunk("UNKN").ConvertToPng());
		Compare(Pattern(2,2),DecodeOutput(WithChunk("tEXt").ConvertToPng(),2,2),2);
	}

	[TestMethod]
	public void Png_TruncatedAndMalformedContents_FailWithoutRuntimeIndexErrors()
	{
		var source=Pack(13,7,Pattern(13,7),4,false);
		for(var length=8;length<source.Length;length+=Math.Max(1,source.Length/37))
		{
			var cut=source[..length];
			try
			{
				cut.ConvertToPng();
				Assert.Fail($"Truncated PNG accepted at {length} bytes.");
			}
			catch(InvalidDataException) {}
			catch(NotSupportedException) {}
		}

		ThrowsExactly<NotSupportedException>(()=>
			RawPng(1,1,4,6,0,[0,0,0,0,0],[],[]).ConvertToPng());
		ThrowsExactly<NotSupportedException>(()=>
			RawPng(1,1,8,6,2,[0,0,0,0,0],[],[]).ConvertToPng());
		ThrowsExactly<InvalidDataException>(()=>
			RawPng(2,1,8,3,0,[0,1,2],[255,0,0],[]).ConvertToPng());
		ThrowsExactly<InvalidDataException>(()=>
			RawPng(1,1,8,6,0,[0,0,0,0,0,0,255],[],[]).ConvertToPng());
	}

	[TestMethod]
	public void Png_IendMustBeLastChunk_NotSilentlyIgnoreAppendedData()
	{
		var source=Pack(2,2,Pattern(2,2),0,false);
		ThrowsExactly<InvalidDataException>(() => source.Concat(new byte[] { 1, 2, 3, 4 }).ToArray().ConvertToPng());
	}

	private static byte[] PackSamples(ReadOnlySpan<int> samples,int depth)
	{
		var result=new byte[(samples.Length*depth+7)/8];
		for(var i=0;i<samples.Length;i++)
		{
			if(depth==16)
			{
				result[i*2]=(byte)(samples[i]>>8);
				result[i*2+1]=(byte)samples[i];
			}
			else if(depth==8)result[i]=(byte)samples[i];
			else
			{
				var bit=i*depth;
				result[bit/8]|=(byte)(samples[i] << (8-depth-bit%8));
			}
		}
		return result;
	}

}
