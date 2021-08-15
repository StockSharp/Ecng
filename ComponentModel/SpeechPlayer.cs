namespace Ecng.ComponentModel
{
	using Ecng.Common;

	[System.Obsolete]
	public class SpeechPlayer : Disposable
	{
		public int Volume { get; set; }

		public void Say(string text) { }
	}
}