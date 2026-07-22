using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MSLX.Plugin.DDNS.Core;

public static class CryptoHelper
{
    private static readonly byte[] KeyHash;

    static CryptoHelper()
    {
        using var sha256 = SHA256.Create();
        KeyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes("mslx_ddns_plugin_secret_salt_2026"));
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        try
        {
            using var aes = Aes.Create();
            aes.Key = KeyHash;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return plainText;
        }
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = KeyHash;
            
            var iv = new byte[aes.BlockSize / 8];
            if (fullCipher.Length < iv.Length) return cipherText;
            
            Array.Copy(fullCipher, iv, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            // 如果解密失败，可能是旧的明文配置，直接返回
            return cipherText;
        }
    }
}
