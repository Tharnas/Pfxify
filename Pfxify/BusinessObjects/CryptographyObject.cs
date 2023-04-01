using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;
using System.ComponentModel;

namespace Pfxify.BusinessObjects
{
    internal interface ICryptographyObject
    {
        string Type { get; }

        string Name { get; }
    }
    public class CertificateCryptographyObject : ICryptographyObject
    {
        public required X509Certificate Certificate { get; init; }

        public string Type => "Zertifikat";

        public string Name => Certificate.SubjectDN.ToString();
    }
    public class PrivateKeyCryptographyObject : ICryptographyObject
    {
        public required AsymmetricKeyParameter PrivateKey { get; init; }

        public string Type => "Privater Schlüssel";

        public string Name => "Schlüssel";
    }

}
