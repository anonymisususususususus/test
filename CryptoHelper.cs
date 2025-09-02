using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RobloxAccountManager
{
    public static class CryptoHelper
    {
        public static byte[] GenerateRandomKey(int length = 32)
        {
            byte[] key = new byte[length];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        public static byte[] DeriveKey(string password, byte[] salt, int keySize = 32)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keySize);
        }

        public static byte[] Encrypt(string plainText, byte[] key, byte[] nonce)
        {
            using var aes = new AesGcm(key);
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16];

            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(nonce.Length);
            bw.Write(nonce);
            bw.Write(tag.Length);
            bw.Write(tag);
            bw.Write(ciphertext.Length);
            bw.Write(ciphertext);
            return ms.ToArray();
        }

        public static string Decrypt(byte[] encryptedData, byte[] key)
        {
            using var ms = new MemoryStream(encryptedData);
            using var br = new BinaryReader(ms);

            int nonceLen = br.ReadInt32();
            byte[] nonce = br.ReadBytes(nonceLen);

            int tagLen = br.ReadInt32();
            byte[] tag = br.ReadBytes(tagLen);

            int cipherLen = br.ReadInt32();
            byte[] ciphertext = br.ReadBytes(cipherLen);

            byte[] plaintext = new byte[cipherLen];
            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
