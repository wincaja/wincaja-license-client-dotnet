using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace WincajaLicenseManager.Core
{
    internal class SecureStorage
    {
        private readonly string _storageDirectory;
        private readonly string _licenseFilePath;
        private readonly byte[] _encryptionKey;

        public SecureStorage()
        {
            // Use AppData folder for storage
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _storageDirectory = Path.Combine(appDataPath, "Wincaja");
            _licenseFilePath = Path.Combine(_storageDirectory, "license.dat");

            // TODO: Generate a proper key derivation from machine-specific data
            // For now, using a combination of machine name and a fixed salt
            _encryptionKey = DeriveKeyFromMachineData();
        }

        private byte[] DeriveKeyFromMachineData()
        {
            // Combine multiple machine-specific elements for key derivation
            var machineData = new StringBuilder();
            machineData.Append(Environment.MachineName);
            machineData.Append(Environment.UserDomainName);
            machineData.Append(Environment.OSVersion.VersionString);
            
            // Add Windows Product ID if available
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productId = key.GetValue("ProductId") as string;
                        if (!string.IsNullOrEmpty(productId))
                        {
                            machineData.Append(productId);
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry access errors
            }

            // Use a fixed salt combined with machine data
            var salt = Encoding.UTF8.GetBytes("WincajaLicense2025!@#");
            
            // Derive a 256-bit key using PBKDF2
            using (var pbkdf2 = new Rfc2898DeriveBytes(machineData.ToString(), salt, 10000))
            {
                return pbkdf2.GetBytes(32); // 256 bits
            }
        }

        public void SaveLicense(object licenseData)
        {
            // Ensure directory exists
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }

            // Serialize license data to JSON
            var json = JsonConvert.SerializeObject(licenseData);
            var plainBytes = Encoding.UTF8.GetBytes(json);

            // Encrypt the data
            var encryptedData = Encrypt(plainBytes);

            // Write to file
            File.WriteAllBytes(_licenseFilePath, encryptedData);
        }

        public T LoadLicense<T>() where T : class
        {
            if (!File.Exists(_licenseFilePath))
            {
                return null;
            }

            try
            {
                // Read encrypted data
                var encryptedData = File.ReadAllBytes(_licenseFilePath);

                // Decrypt the data
                var plainBytes = Decrypt(encryptedData);

                // Deserialize from JSON
                var json = Encoding.UTF8.GetString(plainBytes);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                // Log error or handle as appropriate
                // For now, return null if decryption/deserialization fails
                System.Diagnostics.Debug.WriteLine($"Failed to load license: {ex.Message}");
                return null;
            }
        }

        public bool DeleteLicense()
        {
            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    File.Delete(_licenseFilePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private byte[] Encrypt(byte[] plainBytes)
        {
            // Use the .NET Framework 4.8 compatible implementation
            return AesWithHmac.Encrypt(plainBytes, _encryptionKey);
        }

        private byte[] Decrypt(byte[] encryptedData)
        {
            // Use the .NET Framework 4.8 compatible implementation
            return AesWithHmac.Decrypt(encryptedData, _encryptionKey);
        }

        // Alternative implementation using AES-CBC with HMAC for .NET Framework 4.8 compatibility
        private class AesWithHmac
        {
            public static byte[] Encrypt(byte[] plainBytes, byte[] key)
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new MemoryStream())
                    {
                        // Write IV to the beginning
                        ms.Write(aes.IV, 0, aes.IV.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(plainBytes, 0, plainBytes.Length);
                            cs.FlushFinalBlock();
                        }

                        var ciphertext = ms.ToArray();

                        // Calculate HMAC
                        using (var hmac = new HMACSHA256(key))
                        {
                            var tag = hmac.ComputeHash(ciphertext);
                            
                            // Combine ciphertext + tag
                            var result = new byte[ciphertext.Length + tag.Length];
                            Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
                            Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);
                            
                            return result;
                        }
                    }
                }
            }

            public static byte[] Decrypt(byte[] encryptedData, byte[] key)
            {
                // Extract HMAC tag (last 32 bytes)
                var tagLength = 32; // SHA256 produces 32 bytes
                var tag = new byte[tagLength];
                var ciphertext = new byte[encryptedData.Length - tagLength];
                
                Buffer.BlockCopy(encryptedData, encryptedData.Length - tagLength, tag, 0, tagLength);
                Buffer.BlockCopy(encryptedData, 0, ciphertext, 0, ciphertext.Length);

                // Verify HMAC
                using (var hmac = new HMACSHA256(key))
                {
                    var computedTag = hmac.ComputeHash(ciphertext);
                    
                    // Constant-time comparison
                    var valid = true;
                    for (int i = 0; i < tag.Length; i++)
                    {
                        if (tag[i] != computedTag[i])
                            valid = false;
                    }
                    
                    if (!valid)
                        throw new CryptographicException("HMAC validation failed");
                }

                // Extract IV and decrypt
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    
                    var iv = new byte[aes.BlockSize / 8];
                    Buffer.BlockCopy(ciphertext, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(ciphertext, iv.Length, ciphertext.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var result = new MemoryStream())
                    {
                        cs.CopyTo(result);
                        return result.ToArray();
                    }
                }
            }
        }

    }
}