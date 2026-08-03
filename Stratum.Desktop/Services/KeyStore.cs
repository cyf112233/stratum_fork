using System;
using System.IO;
using System.Security.Cryptography;

namespace Stratum.Desktop.Services
{
    public class KeyStore
    {
        private const int KeyLength = 32;
        private readonly string _dataDirectory;

        public KeyStore(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
        }

        public byte[] GetOrCreateKey()
        {
            if (OperatingSystem.IsWindows())
            {
                return GetOrCreateWindowsKey();
            }

            if (OperatingSystem.IsLinux())
            {
                var secretService = new SecretServiceStore();

                if (secretService.IsAvailable())
                {
                    var key = secretService.GetOrCreateKey(KeyLength);

                    if (key != null)
                    {
                        return key;
                    }
                }
            }

            return GetOrCreateFallbackKey();
        }

        private byte[] GetOrCreateWindowsKey()
        {
            var path = Path.Combine(_dataDirectory, "secret.bin");

            if (File.Exists(path))
            {
                try
                {
                    return ProtectedData.Unprotect(File.ReadAllBytes(path), null,
                        DataProtectionScope.CurrentUser);
                }
                catch
                {
                }
            }

            var key = RandomNumberGenerator.GetBytes(KeyLength);
            var encrypted = ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(_dataDirectory);
            File.WriteAllBytes(path, encrypted);
            return key;
        }

        private byte[] GetOrCreateFallbackKey()
        {
            var path = Path.Combine(_dataDirectory, "secret.key");
            Directory.CreateDirectory(_dataDirectory);

            if (File.Exists(path))
            {
                var key = File.ReadAllBytes(path);

                if (key.Length == KeyLength)
                {
                    return key;
                }
            }

            var newKey = RandomNumberGenerator.GetBytes(KeyLength);
            File.WriteAllBytes(path, newKey);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return newKey;
        }
    }
}
