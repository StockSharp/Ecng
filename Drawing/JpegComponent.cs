namespace Ecng.Drawing;

/// <summary>
/// One component of a JPEG frame: what the frame header says about it and what has been decoded of it.
/// </summary>
internal sealed class JpegComponent
{
	/// <summary>
	/// The identifier scans refer to the component by.
	/// </summary>
	public int Id;

	/// <summary>
	/// Horizontal sampling factor.
	/// </summary>
	public int H;

	/// <summary>
	/// Vertical sampling factor.
	/// </summary>
	public int V;

	/// <summary>
	/// Index of the quantization table.
	/// </summary>
	public int QuantTable;

	/// <summary>
	/// Index of the DC Huffman table the current scan uses.
	/// </summary>
	public int DcTable;

	/// <summary>
	/// Index of the AC Huffman table the current scan uses.
	/// </summary>
	public int AcTable;

	/// <summary>
	/// The DC value of the previous block: differences are coded against it.
	/// </summary>
	public int Prediction;

	public int Precision;
	public int Scale = 1;
	public bool IsDecoded;

	/// <summary>
	/// Width of the component in 8x8 blocks, padded to whole MCUs.
	/// </summary>
	public int BlocksWide;

	/// <summary>
	/// Height of the component in 8x8 blocks, padded to whole MCUs.
	/// </summary>
	public int BlocksHigh;

	/// <summary>
	/// Quantized coefficients, 64 per block in natural order. Only a progressive frame keeps them.
	/// </summary>
	public short[] Coefficients;

	/// <summary>
	/// Decoded samples, <see cref="Stride"/> bytes per row.
	/// </summary>
	public byte[] Samples;

	/// <summary>
	/// Length of a row of <see cref="Samples"/>.
	/// </summary>
	public int BlockSize => 8 / Scale;

	public int Stride => BlocksWide * BlockSize;
}
