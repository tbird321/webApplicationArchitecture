using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ContentManagementApplication.security
{
    public class UserSecurity
    {
        // Read from environment variable TOKEN_SECRET (set in Lambda config or local .env).
        // Falls back to a placeholder — override this in every deployment environment.
        private static string SecretGuid = Environment.GetEnvironmentVariable("TOKEN_SECRET")
            ?? throw new InvalidOperationException("TOKEN_SECRET environment variable is not set. Set it in Lambda environment variables or local dev config.");

        // Read from environment variable TOKEN_IV (16-char string).
        private static string initVector = Environment.GetEnvironmentVariable("TOKEN_IV")
            ?? throw new InvalidOperationException("TOKEN_IV environment variable is not set.");
        private const int keysize = 256;
        private const int EXPIRATION_MINUTES = 60;

        public static string PasswordSalt
        {
            get
            {
                var rng = new RNGCryptoServiceProvider();
                var buff = new byte[32];
                rng.GetBytes(buff);
                return Convert.ToBase64String(buff);
            }
        }

        public static string EncodePassword(string password, string salt)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(password);
            byte[] src = Encoding.Unicode.GetBytes(salt);
            byte[] dst = new byte[src.Length + bytes.Length];
            Buffer.BlockCopy(src, 0, dst, 0, src.Length);
            Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);
            HashAlgorithm algorithm = SHA1.Create();
            byte[] inarray = algorithm.ComputeHash(dst);
            return Convert.ToBase64String(inarray);
        }

        public static String CreateToken(string userName)
        {
            var dateNow = DateTime.UtcNow;
            DateTime authTime = DateTime.Now;
            string TokenString = authTime + "|" + userName;
            string token = EncodeToken(TokenString);
            return token;
        }

        public static Boolean ValidateToken(string token,string userName)
        {
            var dateNow = DateTime.UtcNow;
            DateTime currentTime = DateTime.Now;
            String decrypted = DecryptString(token);
            List<string> parts = new List<string>(decrypted.Split('|'));
            DateTime authTime;
            DateTime.TryParse(parts[0], out authTime);
            bool isValid = false;
            if (parts[1].Equals(userName))
            {
                if (currentTime.Subtract(authTime).TotalMinutes< EXPIRATION_MINUTES)
                {
                    isValid = true;
                }
            }
            return isValid;
        }

        private static string EncodeToken(string plainText)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(SecretGuid, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherTextBytes = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(cipherTextBytes);
        }

        private static string DecryptString(string cipherText)
        {
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            PasswordDeriveBytes password = new PasswordDeriveBytes(SecretGuid, null);
            byte[] keyBytes = password.GetBytes(keysize / 8);
            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        }

    }
}
