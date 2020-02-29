using System;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;

namespace PubnubApi
{
    public class PubnubCrypto : PubnubCryptoBase
    {
        private readonly PNConfiguration config;
        private readonly IPubnubLog pubnubLog;

        public PubnubCrypto(string cipher_key, PNConfiguration pubnubConfig, IPubnubLog log)
            : base(cipher_key)
        {
            this.config = pubnubConfig;
            this.pubnubLog = log;
        }

        public PubnubCrypto(string cipher_key)
            : base(cipher_key)
        {
        }

        protected override string ComputeHashRaw(string input)
        {
            HashAlgorithm algorithm = SHA256.Create();
            Byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);
            return BitConverter.ToString(hashedBytes);
        }

        protected override string EncryptOrDecrypt(bool type, string plainStr)
        {
            //Demo params
            string keyString = GetEncryptionKey();

            Aes aesAlg = Aes.Create();
            aesAlg.KeySize = 256;
            aesAlg.BlockSize = 128;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.IV = System.Text.Encoding.UTF8.GetBytes("0123456789012345");
            aesAlg.Key = System.Text.Encoding.UTF8.GetBytes(keyString);

            if (type)
            {
                // Encrypt
                byte[] cipherText = null;
                plainStr = EncodeNonAsciiCharacters(plainStr);
                ICryptoTransform crypto = aesAlg.CreateEncryptor();
                byte[] plainText = Encoding.UTF8.GetBytes(plainStr);

                cipherText = crypto.TransformFinalBlock(plainText, 0, plainText.Length);

                return Convert.ToBase64String(cipherText);
            }
            else
            {
                try
                {
                    //Decrypt
                    string decrypted = "";
                    byte[] decryptedBytes = Convert.FromBase64CharArray(plainStr.ToCharArray(), 0, plainStr.Length);
                    ICryptoTransform decrypto = aesAlg.CreateDecryptor();

                    var data = decrypto.TransformFinalBlock(decryptedBytes, 0, decryptedBytes.Length);
                    decrypted = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
                    return decrypted;

                }
                catch (Exception ex)
                {
                    if (config != null)
                    {
                        LoggingMethod.WriteToLog(pubnubLog, string.Format("DateTime {0} Decrypt Error. {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), ex), config.LogVerbosity);
                    }
                    throw;
                }
            }
        }

    }
}
