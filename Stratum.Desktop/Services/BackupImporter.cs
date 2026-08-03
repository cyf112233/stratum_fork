using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Backup.Encryption;
using Stratum.Core.Converter;
using Stratum.Core.Service;

namespace Stratum.Desktop.Services
{
    public class BackupImporter
    {
        private readonly IImportService _importService;
        private readonly IRestoreService _restoreService;
        private readonly IIconResolver _iconResolver;

        public BackupImporter(IImportService importService, IRestoreService restoreService,
            IIconResolver iconResolver)
        {
            _importService = importService;
            _restoreService = restoreService;
            _iconResolver = iconResolver;
        }

        public async Task<Backup> TryReadPlainAsync(byte[] data)
        {
            var none = new NoBackupEncryption();

            if (!none.CanBeDecrypted(data))
            {
                return null;
            }

            try
            {
                return await none.DecryptAsync(data, null);
            }
            catch
            {
                return null;
            }
        }

        public bool IsStratumEncrypted(byte[] data)
        {
            return new StrongBackupEncryption().CanBeDecrypted(data) ||
                   new LegacyBackupEncryption().CanBeDecrypted(data);
        }

        public async Task<Backup> TryDecryptStratumAsync(byte[] data, string password)
        {
            var strong = new StrongBackupEncryption();
            var legacy = new LegacyBackupEncryption();

            foreach (var encryption in new IBackupEncryption[] { strong, legacy })
            {
                if (!encryption.CanBeDecrypted(data))
                {
                    continue;
                }

                try
                {
                    return await encryption.DecryptAsync(data, password);
                }
                catch (BackupPasswordException)
                {
                }
            }

            return null;
        }

        public async Task RestoreAsync(Backup backup)
        {
            await _restoreService.RestoreAndUpdateAsync(backup);
        }

        public async Task<ConversionResult> ImportWithConverterAsync(BackupConverter converter, byte[] data,
            string password)
        {
            var (conversion, _) = await _importService.ImportAsync(converter, data, password);
            return conversion;
        }

        public BackupConverter CreateConverter(int formatIndex)
        {
            if (formatIndex < 1 || formatIndex > ImportFormats.All.Count)
            {
                return null;
            }

            return ImportFormats.All[formatIndex - 1].Create(_iconResolver, new CustomIconDecoder());
        }
    }
}
