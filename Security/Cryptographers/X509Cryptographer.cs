namespace Ecng.Security.Cryptographers
{
    using System.Security.Cryptography.X509Certificates;

    public class X509Cryptographer(X509Certificate2 certificate) : AsymmetricCryptographer(certificate.GetRSAPublicKey(), certificate.GetRSAPrivateKey())
    {
	}
}