using System.IO;
using System.Threading.Tasks;
using Stratum.Core;

namespace Stratum.Desktop.Services
{
    public class AssetProvider : IAssetProvider
    {
        private readonly string _baseDirectory;

        public AssetProvider(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public async Task<byte[]> ReadBytesAsync(string path)
        {
            return await File.ReadAllBytesAsync(Path.Combine(_baseDirectory, path));
        }

        public async Task<string> ReadStringAsync(string path)
        {
            return await File.ReadAllTextAsync(Path.Combine(_baseDirectory, path));
        }
    }
}
