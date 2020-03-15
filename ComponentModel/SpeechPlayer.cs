namespace Ecng.ComponentModel
{
	using Ecng.Common;

	public class SpeechPlayer : Disposable
	{
#if NETCOREAPP || NETSTANDARD
		public int Volume { get; set; }

		public void Say(string text) { }
#else
		private readonly System.Speech.Synthesis.SpeechSynthesizer _synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();

		public int Volume
		{
			get => _synthesizer.Volume;
			set => _synthesizer.Volume = value;
		}

		public void Say(string text)
		{
			_synthesizer.Speak(text);
		}
#endif
	}
}