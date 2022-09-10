namespace Ecng.Security
{
	#region Using Directives

	using System.Security.Cryptography.X509Certificates;

	#endregion

	public class X509Cryptographer : AsymmetricCryptographer
	{
		#region X509Cryptographer.ctor()

		public X509Cryptographer(X509Certificate2 certificate)
			: base(certificate.GetRSAPublicKey(), certificate.GetRSAPrivateKey())
		{
		}

		#endregion
	}
}