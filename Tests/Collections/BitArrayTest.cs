namespace Ecng.Tests.Collections
{
	[TestClass]
	public class BitArrayTest
	{
		private void Check(List<bool> bools, List<int> ints, List<long> longs, List<decimal> decs)
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
		public void Large2()
		{
			var stream = new MemoryStream();

			var writer = new BitArrayWriter(stream);
			writer.WriteDecimal(decimal.MinValue);

			var reader = new BitArrayReader(stream);
			reader.ReadDecimal().AssertEqual(decimal.MinValue);
		}
	}
}