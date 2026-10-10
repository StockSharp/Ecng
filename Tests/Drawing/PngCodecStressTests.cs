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
}
