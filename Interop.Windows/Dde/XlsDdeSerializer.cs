namespace Ecng.Interop.Dde
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using Ecng.Common;

	static class XlsDdeSerializer
	{
		private enum DataTypes : short
		{
			Table = 0x0010,
			Float = 0x0001,
			String = 0x0002,
			Bool = 0x0003,
			Error = 0x0004,
			Blank = 0x0005,
			Int = 0x0006,
			Skip = 0x0007,
		}

		private static Type GetCompType(DataTypes type)
		{
			return type switch
			{
				DataTypes.Float => typeof(double),
				DataTypes.String => typeof(string),
				DataTypes.Bool => typeof(bool),
				DataTypes.Int => typeof(int),
				DataTypes.Skip => null,
				_ => throw new ArgumentOutOfRangeException(nameof(type)),
			};
		}

		private static DataTypes GetXlsType(object cell)
		{
			if (cell is null)
				return DataTypes.Blank;

			if (cell is float || cell is double || cell is decimal)
				return DataTypes.Float;

			if (cell is byte || cell is sbyte || cell is short || cell is int || cell is long || cell is ushort || cell is uint || cell is ulong)
				return DataTypes.Int;

			if (cell is string)
				return DataTypes.String;

			if (cell is bool)
				return DataTypes.Bool;

			throw new ArgumentException($"Unknown cell value type '{cell.GetType()}'.", nameof(cell));
		}

		public static byte[] Serialize(IList<IList<object>> rows)
		{
			var stream = new MemoryStream();

			stream.WriteEx(DataTypes.Table);
			stream.WriteEx((short)4);
			stream.WriteEx((short)rows.Count);
			stream.WriteEx((short)(rows.Count == 0 ? 0 : rows[0].Count));

			foreach (var row in rows)
			{
				foreach (var cell in row)
				{
					var cellDt = GetXlsType(cell);

					switch (cellDt)
					{
						case DataTypes.Float:
							stream.WriteEx((short)8);
							stream.WriteEx(cell);
							break;
						case DataTypes.String:
							var str = (string)cell;
							stream.WriteEx((byte)str.Length);
							stream.WriteRaw(str.Default());
							break;
						case DataTypes.Bool:
							stream.WriteEx((short)2);
							stream.WriteEx(cell);
							break;
						case DataTypes.Blank:
							stream.WriteEx((short)2);
							stream.WriteEx(new byte[2]);
							break;
						case DataTypes.Int:
							stream.WriteEx((short)4);
							stream.WriteEx(cell);
							break;
						default:
							throw new ArgumentOutOfRangeException(cellDt.To<string>());
					}
				}
			}

			return stream.ToArray();
		}

		public static IList<IList<object>> Deserialize(byte[] data)
		{
			if (data is null)
				throw new ArgumentNullException(nameof(data));

			var stream = data.To<Stream>();

			var dt = stream.Read<DataTypes>();
			var size = stream.Read<short>();
			var rowCount = stream.Read<short>();
			var columnCount = stream.Read<short>();

			var rows = new List<IList<object>>();

			for (var row = 0; row < rowCount; row++)
			{
				var cells = new List<object>();

				do
				{
					var cellDt = stream.Read<DataTypes>();
					var cellSize = stream.Read<short>();

					if (cellDt != DataTypes.Skip)
					{
						var type = GetCompType(cellDt);
						var typeSize = type == typeof(string) ? cellSize : type.SizeOf();

						var cellColumnCount = cellSize / typeSize;

						for (var column = 0; column < cellColumnCount; column++)
						{
							if (type == typeof(string))
							{
								var pos = 0;
								var buffer = stream.ReadBuffer(typeSize);
								while (pos < buffer.Length)
								{
									var len = buffer[pos];
									var str = new byte[len];
									Buffer.BlockCopy(buffer, pos + 1, str, 0, len);
									cells.Add(Encoding.Default.GetString(str));
									pos += len + 1;
								}
							}
							else
								cells.Add(stream.Read(type));
						}
					}
					else
					{
						stream.ReadBuffer(cellSize);
						cells.AddRange(new object[cellSize]);
					}
				}
				while (cells.Count < columnCount);

				rows.Add(cells);
			}

			return rows;
		}
	}
}