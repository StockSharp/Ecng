namespace Ecng.Tests.Collections;

[TestClass]
public class BitArrayTest
{
	private static void Check(List<bool> bools, List<int> ints, List<long> longs, List<decimal> decs)
	{
		var stream = new MemoryStream();

		using (var writer = new BitArrayWriter(stream))
		{
			foreach (var v in bools)
				writer.Write(v);

			foreach (var v in ints)
				writer.WriteInt(v);

			foreach (var v in longs)
				writer.WriteLong(v);

			foreach (var v in decs)
				writer.WriteDecimal(v);
		}

		//var prevPos = stream.Position;
		stream.Position = 0;

		var reader = new BitArrayReader(stream);

		for (var i = 0; i < bools.Count; i++)
			reader.Read().AssertEqual(bools[i]);

		for (var i = 0; i < ints.Count; i++)
			reader.ReadInt().AssertEqual(ints[i]);

		for (var i = 0; i < longs.Count; i++)
			reader.ReadLong().AssertEqual(longs[i]);

		for (var i = 0; i < decs.Count; i++)
			reader.ReadDecimal().AssertEqual(decs[i]);

		//stream.Position.AssertEqual(prevPos);
	}

	[TestMethod]
	public void Random()
	{
		var bools = new List<bool>();

		for (var i = 0; i < 1000; i++)
			bools.Add(RandomGen.GetBool());

		var ints = new List<int>();

		for (var i = 0; i < 1000; i++)
			ints.Add(RandomGen.GetInt());

		var longs = new List<long>();

		for (var i = 0; i < 1000; i++)
			longs.Add(RandomGen.GetInt());

		var decs = new List<decimal>();

		for (var i = 0; i < 100000; i++)
			decs.Add(RandomGen.GetDecimal());

		Check(bools, ints, longs, decs);
	}

	[TestMethod]
	public void RandomCount()
	{
		var bools = new List<bool>();

		var count = RandomGen.GetInt(1000, 10000);
		for (var i = 0; i < count; i++)
			bools.Add(RandomGen.GetBool());

		var ints = new List<int>();

		count = RandomGen.GetInt(1000, 10000);
		for (var i = 0; i < count; i++)
			ints.Add(RandomGen.GetInt());

		var longs = new List<long>();

		count = RandomGen.GetInt(1000, 10000);
		for (var i = 0; i < count; i++)
			longs.Add(RandomGen.GetInt());

		var decs = new List<decimal>();

		count = RandomGen.GetInt(1000, 10000);
		for (var i = 0; i < count; i++)
			decs.Add(RandomGen.GetDecimal());

		Check(bools, ints, longs, decs);
	}

	[TestMethod]
	public void Large()
	{
		var bools = new List<bool>();

		for (var i = 0; i < 1000; i++)
			bools.Add(RandomGen.GetBool());

		var ints = new List<int>();

		for (var i = 0; i < 1000; i++)
			ints.Add(RandomGen.GetBool() ? int.MaxValue : int.MinValue + 1);

		var longs = new List<long>();

		for (var i = 0; i < 1000; i++)
			longs.Add(RandomGen.GetBool() ? long.MaxValue : long.MinValue + 1);

		var decs = new List<decimal>();

		for (var i = 0; i < 100000; i++)
			decs.Add(RandomGen.GetBool() ? decimal.MaxValue : decimal.MinValue);

		Check(bools, ints, longs, decs);
	}

	[TestMethod]
	public void RandomMixedTypes()
	{
		var actions = new List<Action<BitArrayWriter, object>>
		{
			(w, v) => w.Write((bool)v),
			(w, v) => w.WriteInt((int)v),
			(w, v) => w.WriteLong((long)v),
			(w, v) => w.WriteDecimal((decimal)v)
		};
		var readers = new List<Func<BitArrayReader, object>>
		{
			r => r.Read(),
			r => r.ReadInt(),
			r => r.ReadLong(),
			r => r.ReadDecimal()
		};

		var values = new List<(int typeIdx, object value)>();

		for (var i = 0; i < 10000; i++)
		{
			var typeIdx = RandomGen.GetInt(0, 3);

			object value = typeIdx switch
			{
				0 => RandomGen.GetBool(),
				1 => RandomGen.GetInt(),
				2 => (long)RandomGen.GetInt(),
				3 => RandomGen.GetDecimal(),
				_ => throw new InvalidOperationException()
			};

			values.Add((typeIdx, value));
		}

		var stream = new MemoryStream();

		using (var writer = new BitArrayWriter(stream))
		{
			foreach (var (typeIdx, value) in values)
				actions[typeIdx](writer, value);
		}

		stream.Position = 0;
		var reader = new BitArrayReader(stream);

		foreach (var (typeIdx, value) in values)
			readers[typeIdx](reader).AssertEqual(value);
	}

	[TestMethod]
	public void Large2()
	{
		var stream = new MemoryStream();

		using (var writer = new BitArrayWriter(stream))
			writer.WriteDecimal(decimal.MinValue);

		stream.Position = 0;

		var reader = new BitArrayReader(stream);
		reader.ReadDecimal().AssertEqual(decimal.MinValue);
	}

	/// <summary>
	/// BUG: BitArrayWriter.WriteInt doesn't handle int.MinValue.
	/// Negating int.MinValue overflows back to int.MinValue (negative).
	/// </summary>
	[TestMethod]
	public void WriteInt_ShouldHandleMinValue()
	{
		using var stream = new MemoryStream();

		// This should not corrupt data or throw
		try
		{
			using (var writer = new BitArrayWriter(stream))
			{
				writer.WriteInt(int.MinValue);
			}

			stream.Position = 0;
			var reader = new BitArrayReader(stream);
			var result = reader.ReadInt();

			result.AssertEqual(int.MinValue, "int.MinValue should round-trip correctly");
		}
		catch (OverflowException)
		{
			Fail("WriteInt should handle int.MinValue without overflow");
		}
	}

	/// <summary>
	/// BUG: BitArrayWriter.WriteLong doesn't handle long.MinValue.
	/// Math.Abs(long.MinValue) throws OverflowException.
	/// </summary>
	[TestMethod]
	public void WriteLong_ShouldHandleMinValue()
	{
		using var stream = new MemoryStream();

		try
		{
			using (var writer = new BitArrayWriter(stream))
			{
				writer.WriteLong(long.MinValue);
			}

			stream.Position = 0;
			var reader = new BitArrayReader(stream);
			var result = reader.ReadLong();

			result.AssertEqual(long.MinValue, "long.MinValue should round-trip correctly");
		}
		catch (OverflowException)
		{
			Fail("WriteLong should handle long.MinValue without overflow");
		}
	}
}