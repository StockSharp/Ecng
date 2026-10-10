namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// JPEG decoding checked against pixels that libjpeg-turbo produced for the same files.
/// </summary>
[TestClass]
public class JpegCodecTests : BaseTestClass
{
	// 32x24 grayscale progressive JPEG written by libjpeg-turbo.
	private static readonly byte[] _progressiveGrayA = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEYGh0dHx8fExciJCIeJBweHx7/wgALCAAYACABAREA/8QAGQABAAIDAAAAAAAAAAAAAAAABQAEAQID/9oACAEBAAAAAXMG1XIVw2tIOf/EABwQAAICAgMAAAAAAAAAAAAAAAIDAAQBEBIUMv/aAAgBAQABBQLVn3t2eRzArHCguOgLqrnRrT//xAAmEAABAwICCwAAAAAAAAAAAAABAAIDIZESFCIjMDEzQnJzkqHR/9oACAEBAAY/AthqzJD0GljREMwStG/ld8JshmmTQdwaPkKC64ftf//EACEQAQAABQUAAwAAAAAAAAAAAAEQESFBUQAxYXGBkcHw/9oACAEBAAE/IWVp+kBT1ARmCUo8QmqbFCFYbgqeJw8PvWY5rGMjz1Z9SrdL2q1rIfkn+y1//9oACAEBAAAAEPW//8QAIBABAQACAgEFAQAAAAAAAAAAAREhQQAQgTFRYdHh8P/aAAgBAQABPxDPmiGhszt3fz06IYAufz0siXAbUGPhHzxdvDWXoPaAsY2AVuVFtzmr7DFjG0ygeQ0eHgQpyJCklAoYTg/l93P/2Q==");

	// The same picture decoded by libjpeg-turbo: one byte per pixel, zlib.
	private const string _progressiveGrayAPixels =
		"eNpNkdlu00AUhh+BC9pCCgrLi/EUKKheZomdJs1SFTUpdTzj2TyOHcRywyUIkBAIsVQIlTQJQUXc8BTYwUu+S38z/3/OOOY60ESUcH8ATIwMkMNGWnIdll7yoWNBXGhAXvnyKZWs9OG4bVoIFZ5KEowircoANW4bJoCFf7ZQ8XMiWFVwum/ZsMr3hSQTXWn5qA1Q1T+OaTClglfe64CN+RIvZoKrzf6OvZHPZcylliwM1f/10vksC5YHiOIR40qHSq7TBR+5FijXAzL2E85kxtorcogsWHmtmJBa0Hw4qb20HiO78IGfEDFZX17Hh0PXhhiWnqsnIZciymz2e+ghBtXrAhAlg8eMZS7L4Ox4f719eUKIe5NAaJp1pzroYRNi224W/mpNBCqYMqp5zNhpB9vIaDYcB+Uk7Fq9HjI+5ZyocXfvQTp6sw36g5xhFPqTmzs7VyIZhN37PfSwdzQ8/vTlc07PO5GU8jjZ3r3e3GseeB/ffnv94ff3RU6r0SdEUP8o9Drb9dsv381Xl3/Of1wucwBGlgMdeIAd9+TN+1t3a1tnX/8uZsV9o9VoQ8d0Qcvuv1il3y9Wd25s/Zr9zHEbXcNE2GjZjdHZLA08ny+XF7Xdec4/SJ/Etw==";

	// 32x32 grayscale progressive JPEG written by libjpeg-turbo.
	private static readonly byte[] _progressiveGrayB = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEYGh0dHx8fExciJCIeJBweHx7/wgALCAAgACABAREA/8QAGgAAAgMBAQAAAAAAAAAAAAAAAwcCBQYBBP/aAAgBAQAAAAHkWdJYlYpFiOvqtT486D//xAAcEAACAwADAQAAAAAAAAAAAAACAwABBAUQEhP/2gAIAQEAAQUCgJprxqhHpZeGLOmB05yEBfN6BWXI7iuakU8WZHDPi2f/xAAnEAACAgAEAwkAAAAAAAAAAAABAgARAwQQMRMiwTIzQVFxkZKh8P/aAAgBAQAGPwKKLqzAo2Gqv5G4GU2DqGxGYk7Ko/dZwsuq4a+BPMw6fUs5rF+Wm9ETs36Tu29p/8QAIBABAAAGAQUAAAAAAAAAAAAAAQAQESExQYFRYXGh8P/aAAgBAQABPyGDHZxtBgAFAmwGSgVQGSdZm2Xt1Wx5OENtknuPnteGt/a0EHAYkdHRMVfCbk9//9oACAEBAAAAEPna/8QAHRABAAMBAAIDAAAAAAAAAAAAAQARITFBYVGB4f/aAAgBAQABPxDxLZhlo1C4bsACgD4JbRPHIaIkL9MsnmOE9WbM5Uuritk9FYXKClq4ptdIWtg6ad6c4wuUkQ3bfAD0AHCNUL4izthab0qNsztbv67H9TP/2Q==");

	private const string _progressiveGrayBPixels =
		"eNpNkt9OE0EUxt/CBzCRxNRud2d3+4cWKX8SgtHEC/XKCwUhUBCkWJDu7sxst1sINiH4CPIK3pIYjZBoIkiCCrS7MzvbRhMewpndFjybnZvfme+c78sghHkhZCw/nZ2aflFqHnRDRi4oDRgLeKEeR9W5+eez8+X9VnhOGOkQwXlXn9tOZenZ9EzlhJJW2KUhoUFACaV9Dh1YXSi9bB63g6BDvdDnjBByzRF2LbOx7wUs8C+8bsR9Qq70TasG8c7vDhcVc3v3/+fmBtw+9dqEhZ3Q86P51/omrCGr3jwJWJe1PG6xxyGKCto2NNHG7qnYKWDUE4wx6l/5x8LC7lHIBCGeMMc54dC2+cF1sIub+z6lvs+56BITouuRgI0cVNs7bfEJAfVIXH5/PkY1/pk7J+2Qe+MOqfBHKYwaxOnwjK3G4VmHtUVIcfxMbIYjiTpELnbef2wFvs8n8xY+iF3Nx3XLcjGq7h2JvXxfmPBIYAlpCC1YrcGGY2DbOWLnP44PD351zr5//fINxv7hdn1tyzQct/pu68PnT95Z8PjBo/EnkxPCvghgHZsN026sLJbaLe/vzXxS1RJqWhru52PXME9g4fLPz5ykJAczal5TlVRGiXfD2Nh0l2nQzkh3szlNu5XTb+RHEkoS9Mzzal8uFkYz2h0lJWeBliukbmWG5DSOOX6LNirjQwMyyIKckkgnpdztQn5Al8TuwgOs4koBjEkqkOREWtaAnpWUQZBCfDHLsPhfL48p6aSmK7KuA00VBQBAmHMTYte0lia1tCbrqqoDSdG0uAVye/zx2g7C5syEruhZNSXLSkbrNQiOEbardr1hz94blQBQs2lZAUCJCsXx4A0HmS5+VZoYUZNAlYGu60JCQ1EDf7+b+E1tE5trcw+LxYIuVosUovAQtJDr8DdsGdBaW12Zuj88IqpYLMbcMsQb4T4cnrRho/XXq1GVy/8APHombg==";

	// 32x32 grayscale baseline JPEG.
	private static readonly byte[] _baselineGray = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/wAALCAAgACABAREA/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEBAAA/AE/hqDSvBMHijxVptskv2Vru4SJ22bl5ZVzX6CafYxabZW9pBGqQQoqIijaoUf3Vqbcdq1+ffb7tX9D1J9F1iy1CMBpbaZJhu9mr7m0TWLbxBpVvf2c6T286bhJH92r/ALZHzV+ffH3cVS8UeMPDHgPSILzWb+6urq43tDpmmW+52KfeWWV9qRhvl2uizY3MWT5cNxFz+214t0nRpdE8J2VroenZVoJ7x2vr2H5lZ/mO2A7vmX/j3HDf3/nrzvUP2iPibqVzLcXHjnXNzuzmOK9eKJcnd8iJtCD/AGVUAfdWvohsYUn+GuV8f+AbbxxYRRySm3uofmjkX5l5+8u2vENZ+EfiTSWlIs/tkSY+e2O7P/AetYzeC9e/6A97/wB+DX//2Q==");

	private const string _baselineGrayPixels =
		"eNplUtlu00AU/QU+AQlVUJKmXmKnC92hbE9FvCBeqLrSCorSLYntuTO2m1QpiO0Tyi/wWIlFVIBEgVKktE3iGTtdxAMgvoA76SKWI9nzcO7cc+6ZC0ARANnpm8ODI0O3i6v7OzVR5jyMolCIEA55Yo+OD45PzLyoRKVqJGpc/M27/szdW8PDc+uCl2u7ohaIMBSciyMefGJPTk4srZWjnT1R2Ql4GPI/eIe4eQsWVngt4sFWeT/gQvCA8yOeWeDmvIelvahSwZviX961iDNLF79WqoHY3Y+q//XPecwuFDeC3X1RDqK6vvRHCHoDQnwKNsk93uAcnUciCLE5ntWj+9QB4sKTNQwHpXmAlzEBESDJGP6ITcFn91cCIQK0VcXeUkHyByAMPGDLX7alK17hh6jLowGKrAu5R5/KtRCVsUZIe+iPSHsEiEeYZxXebe2G6D9CkzL+CI7hOTTveM9elkWV1ykcQUTH+tS3Ie/Q7PKafLmKHKKKhQ7BAYE4bJ7SBZ/aHqyHm+uf378p1bY/vl39QA7mJwuLmXzOJsXc08Xnr1+VNqLrA9d6bvSfh4P5YS7rFG2vcG98Yqsc/DrZFdeVxlRLou9IfeHBPHVz49+/bXY0NSa0DrPTSDXFuhL13cAmGVga+RGV2hLtRpvR3mAYJ1r61FSsWa4eJkBdCH4OtV3RDVXREnpLsrVHaTCS8Q4qw0GTRZjPdHXFEs1tSqvamIrFUqe7W85cOMVkAVBmWWy2Q+1UNK05frY9njIMU9HNZp0wl9qWg5+f7tPMmJbUNCOpqrquaxLIM8cmLO9Ydy4nzZRuaKquqck6sIhQlxEHmIcrNtav6YapK6qqGvphA4LxAAGWYX7BH73Yk8DWKVNVDlhVBRkfzjfvQ9anU5OXuvU4koZpmoZEPX75QItsDgrMnhkb6Os+hyYQCoLWF5Q48vEoIxlw5tJTg1c7e3t7exDddR4cm+EMts08BjmL0uzsNCKdTqd/A/oFJkg=";

	// 16x16 baseline JPEG whose three components are R, G and B: Adobe APP14 with transform 0, component ids 'R', 'G', 'B'.
	private static readonly byte[] _rgbJpeg = Convert.FromBase64String(
		"/9j/7gAOQWRvYmUAZAAAAAAA/9sAQwADAgICAgIDAgICAwMDAwQGBAQEBAQIBgYFBgkICgoJCAkJCgwPDAoLDgsJCQ0RDQ4PEBAREAoMEhMSEBMPEBAQ/8AAEQgAEAAQA1IRAEcRAEIRAP/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/aAAwDUgBHAEIAAD8A+VP2bf8Al1/4DXyp/wAM2/8ATh/47X178SfiT/rf9I9e9fqt+zb/AMuv/AaP+Gbf+nD/AMdr5V+JPxJ/1v8ApHr3r8qf2bf+XX/gNfqt/wAM2/8ATh/47R8SfiT/AK3/AEj171+q37Nv/Lr/AMBo/wCGbf8Apw/8dr5U+JPxJ/1v+kevev/Z");

	// Three bytes per pixel, zlib. The CMYK fixtures below were made from the same picture.
	private const string _colorPixels =
		"eNoFwSGAlEAUANA6zTi0a94kNw7JtS0U18Y0bPwkNqZxjZ8OG79hkp+kOSTX5EJx407yTB6kizdN3xPiXySelXjS4jEVv424grhYcUZxIuFYjE7wInovulW0QTRCPkfySclHLf+k8mrkBeTZyhNKR3JkOTj5eZGdl+0qmyBroZ4i9VepB62uqfpl1BnUyapvqEZSA6veqW5R9141q6qDqkT8GMUPKr7q+JLGP018gtjZeMR4oLjnuHPxpyVufFyvcRXiUiQPUXJVyUUn5zT5bhIHyWiTAZOeko6T1iW4JLVPqjUpQ1IIc43MRZmzNj9S44wZwQzW9GiITMumceZuMZU35WogmFzAJYKzgpMGl8JXAwNAb6FDaAkahtpBtcAHD8UKeYBM2HNkT8o6bcfUfjG2B9tZ26JtyNZsK2c/LrbwNl9tFuxR4ClCp3DUyCn2BjvA1mKDWBNWjKVDWDD3mK14DHgQ5CIaFQ2aPqfUGWqBGks1UkVUMhWO3i+UeTqudAi0FzxGPCjuNXcp3xtugGvLFXJJXDDnjrOF33o+rLwPrMU0RFOvpk5Pn9KpMVMNU2WnEqeCppynzE3vlungp/066TDtxNxHc6fmVs+YzrWZK5hLOxc45zRnPB/dfFjm137W67wL863wFPlW+Ub7u9RXxpfgwfocfUb+yP7g/JvFa+93q78N/kZsbbQ1aqv1VqXbB7MVsOV2y3A70nbgbe82vWyv/Ha7bjdhkyI0UahVqHT4mIbChBxCZsMRw4HCnoN2YbeElz7crEGG8OI/YEWHFg==";

	// 16x16 Adobe CMYK JPEG, baseline.
	private static readonly byte[] _cmykBaseline = Convert.FromBase64String(
		"/9j/7gAOQWRvYmUAZAAAAAAA/9sAQwADAgICAgIDAgICAwMDAwQGBAQEBAQIBgYFBgkICgoJCAkJCgwPDAoLDgsJCQ0RDQ4PEBAREAoMEhMSEBMPEBAQ/8AAFAgAEAAQBEMRAE0RAFkRAEsRAP/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/aAA4EQwBNAFkASwAAPwD5U/Zt/wCXX/gNfKn/AAzb/wBOH/jtfXvxJ+JP+t/0j171+qdfqt+zb/y6/wDAaP8Ahm3/AKcP/Ha+VfiT8Sf9b/pHr3or8qf2bf8Al1/4DX6rf8M2/wDTh/47R8SfiT/rf9I9e9Ffqt+zb/y6/wDAaP8Ahm3/AKcP/Ha+VPiT8Sf9b/pHr3or/9k=");

	// 16x16 Adobe CMYK JPEG, progressive.
	private static readonly byte[] _cmykProgressive = Convert.FromBase64String(
		"/9j/7gAOQWRvYmUAZAAAAAAA/9sAQwADAgICAgIDAgICAwMDAwQGBAQEBAQIBgYFBgkICgoJCAkJCgwPDAoLDgsJCQ0RDQ4PEBAREAoMEhMSEBMPEBAQ/8IAFAgAEAAQBEMRAE0RAFkRAEsRAP/EABYAAQEBAAAAAAAAAAAAAAAAAAAHCP/aAA4EQwBNAFkASwAAAAGUymvap1WlTKmqzVaVP//EABUQAQEAAAAAAAAAAAAAAAAAAAAG/9oACAFDAAEFAptNptNv/8QAFRABAQAAAAAAAAAAAAAAAAAAABb/2gAIAU0AAQUCm02m02//xAAVEAEBAAAAAAAAAAAAAAAAAAAFAP/aAAgBWQABBQJJKSSkkpJK/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBSwABBQIf/8QAFRABAQAAAAAAAAAAAAAAAAAAAAL/2gAIAUMABj8ClKUv/8QAFRABAQAAAAAAAAAAAAAAAAAAADL/2gAIAU0ABj8ChCEP/8QAFhAAAwAAAAAAAAAAAAAAAAAAAAEi/9oACAFZAAY/AnQ6HQ6P/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBSwAGPwIf/8QAFRABAQAAAAAAAAAAAAAAAAAAAMH/2gAIAUMAAT8hmmmm/8QAFhAAAwAAAAAAAAAAAAAAAAAAABHw/9oACAFNAAE/IbRaLRaP/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBWQABPyEAAB//xAAUEAEAAAAAAAAAAAAAAAAAAAAg/9oACAFLAAE/IR//2gAOBEMATQBZAEsAAAAQ/wD9/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBQwABPxAAAf/EABQQAQAAAAAAAAAAAAAAAAAAACD/2gAIAU0AAT8QCSX/xAAWEAADAAAAAAAAAAAAAAAAAAAAQaH/2gAIAVkAAT8QvMvMvMvM/8QAFBABAAAAAAAAAAAAAAAAAAAAIP/aAAgBSwABPxAf/9k=");

	private const byte _adobeMarker = 0xEE;

	[TestMethod]
	public void Progressive_RunOfSixteenZerosInARefinementScan_DecodesAsLibjpegDoes()
	{
		AssertGray(_progressiveGrayA, _progressiveGrayAPixels, 32, 24);
		AssertGray(_progressiveGrayB, _progressiveGrayBPixels, 32, 32);
	}

	[TestMethod]
	public void Baseline_GrayWithSamplingFactorsOtherThanOne_KeepsBlocksInPlace()
	{
		foreach (var factors in new byte[] { 0x11, 0x22, 0x21, 0x12, 0x41 })
			AssertGray(WithSamplingFactors(_baselineGray, factors), _baselineGrayPixels, 32, 32);
	}

	[TestMethod]
	public void Progressive_GrayWithSamplingFactorsOtherThanOne_KeepsBlocksInPlace()
	{
		foreach (var factors in new byte[] { 0x22, 0x21, 0x12 })
			AssertGray(WithSamplingFactors(_progressiveGrayB, factors), _progressiveGrayBPixels, 32, 32);
	}

	[TestMethod]
	public void ThreeComponentsMarkedAsRgb_AreNotConvertedFromYCbCr()
	{
		AssertColor(_rgbJpeg, 3);

		// Without the Adobe segment the component ids 'R', 'G', 'B' say the same.
		AssertColor(RemoveJpegSegments(_rgbJpeg, _adobeMarker), 3);
	}

	[TestMethod]
	public void Cmyk_BaselineAndProgressive_MatchTheReference()
	{
		AssertColor(_cmykBaseline, 3);
		AssertColor(_cmykProgressive, 3);
	}

	[TestMethod]
	public void Cmyk_WithoutTheAdobeMarker_IsStillReadAsInverted()
	{
		AssertColor(RemoveJpegSegments(_cmykBaseline, _adobeMarker), 3);
		AssertColor(RemoveJpegSegments(_cmykProgressive, _adobeMarker), 3);
	}

	[TestMethod]
	public void Progressive_CoefficientThatDoesNotFitSixteenBits_IsInvalidData()
	{
		// One 8x8 block: a DC difference of 2047 shifted left by Al = 13 under a 16-bit quantizer of 65535.
		byte[] jpeg =
		[
			0xFF, 0xD8,
			0xFF, 0xDB, 0x00, 0x83, 0x10, .. Enumerable.Repeat((byte)0xFF, 128),
			0xFF, 0xC2, 0x00, 0x0B, 8, 0, 8, 0, 8, 1, 1, 0x11, 0,
			0xFF, 0xC4, 0x00, 0x14, 0x00, 1, .. new byte[15], 11,
			0xFF, 0xDA, 0x00, 0x08, 1, 1, 0x00, 0, 0, 0x0D,
			0x7F, 0xFF, 0x00,
			0xFF, 0xD9,
		];

		ThrowsExactly<InvalidDataException>(() => jpeg.ConvertToPng());
	}

	[TestMethod]
	public void Progressive_ThreeFullResolutionComponentsUnderTheSizeLimit_AreAccepted()
	{
		// 4096x3280 = 13.4 megapixels, 4:4:4: three planes of 209 920 blocks each.
		const int width = 4096;
		const int height = 3280;
		const int blocks = width / 8 * (height / 8);

		// A DC table whose only code is one bit long and stands for "no difference": a DC-only scan of
		// three interleaved components is then three zero bits per MCU.
		byte[] jpeg =
		[
			0xFF, 0xD8,
			0xFF, 0xDB, 0x00, 0x43, 0x00, .. Enumerable.Repeat((byte)1, 64),
			0xFF, 0xC2, 0x00, 0x11, 8, height >> 8, height & 0xFF, width >> 8, width & 0xFF, 3, 1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0,
			0xFF, 0xC4, 0x00, 0x14, 0x00, 1, .. new byte[15], 0,
			0xFF, 0xDA, 0x00, 0x0C, 3, 1, 0x00, 2, 0x00, 3, 0x00, 0, 0, 0x00,
			.. new byte[(blocks * 3 + 7) / 8],
			0xFF, 0xD9,
		];

		var result = jpeg.ResizeImage(4, 4);

		// Every coefficient is zero: mid-gray after the level shift.
		var (w, h, rgba) = ReadPng(result.ConvertToPng());
		w.AssertEqual(4);
		h.AssertEqual(3);
		AssertPixels(Rgba(4, 3, (_, _) => (128, 128, 128, 255)), rgba, 4, tolerance: 2);
	}

	[TestMethod]
	public void FileTooShortForTheSizeItDeclares_IsRefusedBeforeItsBuffersAreAllocated()
	{
		// Baseline and progressive: tables, a 5000x5000 frame of three components, a scan of sixteen bytes.
		foreach (var (frame, lastCoefficient) in new (byte, byte)[] { (0xC0, 63), (0xC2, 0) })
		{
			byte[] jpeg =
			[
				0xFF, 0xD8,
				0xFF, 0xDB, 0x00, 0x43, 0x00, .. Enumerable.Repeat((byte)1, 64),
				0xFF, frame, 0x00, 0x11, 8, 0x13, 0x88, 0x13, 0x88, 3, 1, 0x22, 0, 2, 0x11, 0, 3, 0x11, 0,
				0xFF, 0xC4, 0x00, 0x14, 0x00, 1, .. new byte[15], 0,
				0xFF, 0xC4, 0x00, 0x14, 0x10, 1, .. new byte[15], 0,
				0xFF, 0xDA, 0x00, 0x0C, 3, 1, 0x00, 2, 0x00, 3, 0x00, 0, lastCoefficient, 0x00,
				.. new byte[16],
				0xFF, 0xD9,
			];

			var before = GC.GetAllocatedBytesForCurrentThread();

			ThrowsExactly<InvalidDataException>(() => jpeg.ConvertToPng());

			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			(allocated < 1_000_000).AssertTrue($"{allocated} bytes were allocated for a file of {jpeg.Length}");
		}
	}

	private static byte[] WithSamplingFactors(byte[] jpeg, byte factors)
	{
		var result = (byte[])jpeg.Clone();

		// Frame header: precision, height, width, component count, then id / factors / table per component.
		result[FindJpegFrame(result) + 7] = factors;
		return result;
	}

	private static void AssertGray(byte[] jpeg, string pixels, int width, int height)
		=> AssertPixels(GrayToRgba(Inflate(pixels)), ReadPng(jpeg.ConvertToPng(), width, height), width, tolerance: 2);

	private static void AssertColor(byte[] jpeg, int tolerance)
		=> AssertPixels(RgbToRgba(Inflate(_colorPixels)), ReadPng(jpeg.ConvertToPng(), 16, 16), 16, tolerance: tolerance);
}
