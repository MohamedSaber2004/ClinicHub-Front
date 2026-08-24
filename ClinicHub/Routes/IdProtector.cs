using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClinicHub.Routes
{
    /// <summary>
    /// Bidirectional URL-safe tokenization and AES encryption helper for route IDs (Guids and integers)
    /// to hide raw database identifiers from frontend URLs and browser address bars.
    /// </summary>
    public static class IdProtector
    {
        private static readonly byte[] Key = new byte[] { 
            0x2A, 0x5F, 0x81, 0x9B, 0x4C, 0xD2, 0x1E, 0x77, 
            0x33, 0xA8, 0x56, 0xFC, 0x09, 0xE4, 0x61, 0xB2 
        };

        private static readonly byte[] Iv = new byte[] { 
            0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF, 
            0xFE, 0xDC, 0xBA, 0x09, 0x87, 0x65, 0x43, 0x21 
        };

        public static string Protect(Guid id)
        {
            if (id == Guid.Empty) return string.Empty;
            return EncryptBytes(id.ToByteArray());
        }

        public static string Protect(int id)
        {
            return EncryptBytes(BitConverter.GetBytes(id));
        }

        public static string Protect(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (Guid.TryParse(value, out var guid))
            {
                return Protect(guid);
            }
            if (int.TryParse(value, out var num))
            {
                return Protect(num);
            }
            return EncryptBytes(Encoding.UTF8.GetBytes(value));
        }

        public static Guid UnprotectGuid(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return Guid.Empty;

            // Direct parse if already a plain Guid
            if (Guid.TryParse(token, out var directGuid))
            {
                return directGuid;
            }

            try
            {
                var bytes = DecryptBytes(token);
                if (bytes != null && bytes.Length == 16)
                {
                    return new Guid(bytes);
                }
            }
            catch
            {
            }

            return Guid.Empty;
        }

        public static int UnprotectInt(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return 0;

            if (int.TryParse(token, out var directInt))
            {
                return directInt;
            }

            try
            {
                var bytes = DecryptBytes(token);
                if (bytes != null && bytes.Length >= 4)
                {
                    return BitConverter.ToInt32(bytes, 0);
                }
            }
            catch
            {
            }

            return 0;
        }

        private static string EncryptBytes(byte[] raw)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(raw, 0, raw.Length);
                    cs.FlushFinalBlock();
                }

                var encrypted = ms.ToArray();
                return ToBase64Url(encrypted);
            }
            catch
            {
                return ToBase64Url(raw);
            }
        }

        private static byte[]? DecryptBytes(string token)
        {
            try
            {
                var encrypted = FromBase64Url(token);
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encrypted, 0, encrypted.Length);
                    cs.FlushFinalBlock();
                }

                return ms.ToArray();
            }
            catch
            {
                try
                {
                    return FromBase64Url(token);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static string ToBase64Url(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static byte[] FromBase64Url(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
