namespace Ecng.Security.Cryptographers
{
    using System.Security.Cryptography.X509Certificates;

	/// <summary>
	/// X509 cryptographer.
	/// </summary>
	/// <param name="certificate"><see cref="X509Certificate2"/></param>
	public class X509Cryptographer(X509Certificate2 certificate) : AsymmetricCryptographer(certificate.GetRSAPublicKey(), certificate.GetRSAPrivateKey())
    {
	}
}