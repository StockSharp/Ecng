namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Decodes APNG animation without discarding frames. PNG scanline decompression
/// and CRC validation are performed by the existing managed PNG decoder.
/// Supports full PNG color modes, frame subrectangles, SOURCE/OVER blending,
/// NONE/BACKGROUND/PREVIOUS disposal, arbitrary delays and loop counts.
/// </summary>
internal static class ApngDecoder
{
	private sealed class RawFrame
	{
		public int X, Y, Width, Height, Delay, Disposal, Blend;
		public MemoryStream Compressed { get; } = new();
	}

	private static ReadOnlySpan<byte> Signature => [137,80,78,71,13,10,26,10];

	public static bool IsApng(ReadOnlySpan<byte> data)
	{
		if (!data.StartsWith(Signature)) return false;
		for (var pos=8;pos+12<=data.Length;)
		{
			var length = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(pos,4));
			if (length > (uint)(data.Length - pos - 12)) return false;
			var tag = data.Slice(pos+4,4);
			if (tag.SequenceEqual("acTL"u8)) return true;
			if (tag.SequenceEqual("IDAT"u8) || tag.SequenceEqual("IEND"u8)) return false;
			pos += (int)length + 12;
		}
		return false;
	}

	public static GifAnimation Decode(byte[] data)
	{
		if (!data.AsSpan().StartsWith(Signature) || data.Length > 128*1024*1024)
			throw new InvalidDataException("Invalid APNG input.");

		var bytes=data.AsSpan();
		var position=8;
		var header=Array.Empty<byte>();
		var palette=Array.Empty<byte>();
		var transparency=Array.Empty<byte>();
		var width=0;var height=0;
		var expectedCount=0;
		var loopCount=1;
		var expectedSequence=0u;
		var seenIdat=false;
		var seenActl=false;
		var ended=false;
		var frameData=new List<RawFrame>();
		RawFrame active=null;
		long totalPixels=0;

		while(position+12<=data.Length)
		{
			var size=BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(position,4));
			if(size > (uint)(data.Length-position-12))
				throw new InvalidDataException("APNG chunk length exceeds file.");
			var chunk=bytes.Slice(position+4,4);
			var content=bytes.Slice(position+8,(int)size);
			var crc=BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(position+8+(int)size,4));
			if(Crc(bytes.Slice(position+4,(int)size+4))!=crc)
				throw new InvalidDataException("APNG chunk CRC mismatch.");

			if(chunk.SequenceEqual("IHDR"u8))
			{
				if(header.Length>0||position!=8||content.Length!=13)
					throw new InvalidDataException("Invalid APNG IHDR.");
				header=content.ToArray();
				width=BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0,4));
				height=BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4,4));
				if(width<1||height<1||(long)width*height>25_000_000)
					throw new InvalidDataException("APNG dimensions exceed 25 megapixels.");
			}
			else if(chunk.SequenceEqual("PLTE"u8))
				palette=content.ToArray();
			else if(chunk.SequenceEqual("tRNS"u8))
				transparency=content.ToArray();
			else if(chunk.SequenceEqual("acTL"u8))
			{
				if(seenActl||seenIdat||content.Length!=8)
					throw new InvalidDataException("Invalid APNG animation control.");
				seenActl=true;
				var frames=BinaryPrimitives.ReadUInt32BigEndian(content[..4]);
				var loops=BinaryPrimitives.ReadUInt32BigEndian(content[4..]);
				if(frames<1||frames>512||loops>int.MaxValue)
					throw new InvalidDataException("APNG count or loop limit exceeded.");
				expectedCount=(int)frames;
				loopCount=(int)loops;
			}
			else if(chunk.SequenceEqual("fcTL"u8))
			{
				if(!seenActl||content.Length!=26)
					throw new InvalidDataException("Invalid APNG frame control.");
				var sequence=BinaryPrimitives.ReadUInt32BigEndian(content[..4]);
				if(sequence!=expectedSequence++)
					throw new InvalidDataException("APNG frame sequence out of order.");
				var w=BinaryPrimitives.ReadUInt32BigEndian(content.Slice(4,4));
				var h=BinaryPrimitives.ReadUInt32BigEndian(content.Slice(8,4));
				var x=BinaryPrimitives.ReadUInt32BigEndian(content.Slice(12,4));
				var y=BinaryPrimitives.ReadUInt32BigEndian(content.Slice(16,4));
				if(w==0||h==0||w>width||h>height||x>width-w||y>height-h)
					throw new InvalidDataException("APNG frame rectangle exceeds canvas.");
				totalPixels+=(long)w*h;
				if(frameData.Count>=512||totalPixels>40_000_000)
					throw new InvalidDataException("APNG frame pixel budget exceeded.");

				var numerator=BinaryPrimitives.ReadUInt16BigEndian(content.Slice(20,2));
				var denominator=BinaryPrimitives.ReadUInt16BigEndian(content.Slice(22,2));
				if(denominator==0)denominator=100;
				var disposal=content[24];var blend=content[25];
				if(disposal>2||blend>1)
					throw new InvalidDataException("Unsupported APNG disposal/blend operation.");
				active=new RawFrame
				{
					Width=(int)w, Height=(int)h,X=(int)x,Y=(int)y,
					Delay=(int)Math.Round((double)numerator*100/denominator),
					Disposal=disposal,Blend=blend
				};
				frameData.Add(active);
			}
			else if(chunk.SequenceEqual("IDAT"u8))
			{
				if(!seenActl)
					throw new InvalidDataException("APNG control must precede IDAT.");
				seenIdat=true;
				// With fcTL preceding IDAT the default image is frame zero.
				// Otherwise IDAT is a static poster, not part of the animation.
				if(active!=null && frameData.Count==1)
					active.Compressed.Write(content);
			}
			else if(chunk.SequenceEqual("fdAT"u8))
			{
				if(!seenActl||active==null||content.Length<4)
					throw new InvalidDataException("Orphaned APNG frame data.");
				var sequence=BinaryPrimitives.ReadUInt32BigEndian(content[..4]);
				if(sequence!=expectedSequence++)
					throw new InvalidDataException("APNG frame data sequence out of order.");
				active.Compressed.Write(content[4..]);
			}
			else if(chunk.SequenceEqual("IEND"u8))
			{
				if(content.Length!=0||position+12!=data.Length)
					throw new InvalidDataException("Invalid APNG end.");
				ended=true;
				break;
			}
			else if((chunk[0]&0x20)==0)
				throw new NotSupportedException("Unexpected critical APNG chunk.");

			position+=12+(int)size;
		}

		if(!ended||!seenActl||frameData.Count!=expectedCount||header.Length!=13)
			throw new InvalidDataException("Incomplete APNG animation.");

		var animation=new GifAnimation(width,height){LoopCount=loopCount};
		var canvas=new RasterImage(width,height);
		RawFrame previous=null;
		byte[] saved=null;

		foreach(var frame in frameData)
		{
			if(previous!=null)
			{
				if(previous.Disposal==1)
				{
					for(var y=previous.Y;y<previous.Y+previous.Height;y++)
						for(var x=previous.X;x<previous.X+previous.Width;x++)
							Array.Clear(canvas.Pixels,(y*width+x)*4,4);
				}
				else if(previous.Disposal==2 && saved!=null)
					Array.Copy(saved,canvas.Pixels,canvas.Pixels.Length);
			}

			var backup=frame.Disposal==2?(byte[])canvas.Pixels.Clone():null;
			if(frame.Compressed.Length==0)
				throw new InvalidDataException("APNG frame data is missing.");
			var image=DecodeFrame(header,palette,transparency,frame);
			for(var y=0;y<frame.Height;y++)
				for(var x=0;x<frame.Width;x++)
				{
					var src=(y*frame.Width+x)*4;
					var dest=((y+frame.Y)*width+x+frame.X)*4;
					if(frame.Blend==0)
						Array.Copy(image.Pixels,src,canvas.Pixels,dest,4);
					else
						canvas.BlendPixel(x+frame.X,y+frame.Y,
							image.Pixels[src],image.Pixels[src+1],
							image.Pixels[src+2],image.Pixels[src+3]);
				}

			var screenshot=new RasterImage(width,height);
			Array.Copy(canvas.Pixels,screenshot.Pixels,canvas.Pixels.Length);
			animation.Frames.Add(new GifFrame(screenshot,frame.Delay));
			previous=frame;saved=backup;
		}

		return animation;
	}

	private static RasterImage DecodeFrame(byte[] originalHeader,byte[] palette,byte[] transparency,RawFrame frame)
	{
		using var output=new MemoryStream();
		output.Write(Signature);
		var header=(byte[])originalHeader.Clone();
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0,4),frame.Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4,4),frame.Height);
		WriteChunk(output,"IHDR"u8,header);
		if(palette.Length!=0)WriteChunk(output,"PLTE"u8,palette);
		if(transparency.Length!=0)WriteChunk(output,"tRNS"u8,transparency);
		WriteChunk(output,"IDAT"u8,frame.Compressed.ToArray());
		WriteChunk(output,"IEND"u8,[]);
		return PngCodec.Decode(output.ToArray());
	}

	private static void WriteChunk(Stream output,ReadOnlySpan<byte> tag,ReadOnlySpan<byte> content)
	{
		Span<byte> number=stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(number,(uint)content.Length);
		output.Write(number);output.Write(tag);output.Write(content);
		uint crc=0xFFFFFFFF;
		foreach(var b in tag)crc=NextCrc(crc,b);
		foreach(var b in content)crc=NextCrc(crc,b);
		BinaryPrimitives.WriteUInt32BigEndian(number,~crc);
		output.Write(number);
	}
	private static uint Crc(ReadOnlySpan<byte> payload)
	{
		uint crc=0xFFFFFFFF;
		foreach(var b in payload)crc=NextCrc(crc,b);
		return ~crc;
	}
	private static uint NextCrc(uint crc,byte b)
	{
		crc^=b;
		for(var i=0;i<8;i++)
			crc=(crc&1)!=0?(crc>>1)^0xEDB88320:crc>>1;
		return crc;
	}
}
