namespace Ecng.Security.Cryptographers
{
    using System.Security.Cryptography.X509Certificates;

    public class X509Cryptographer : AsymmetricCryptographer
    {
        public X509Cryptographer(X509Certificate2 certificate)
            : base(certificate.GetRSAPublicKey(), certificate.GetRSAPrivateKey())
        {
        }
    }
}