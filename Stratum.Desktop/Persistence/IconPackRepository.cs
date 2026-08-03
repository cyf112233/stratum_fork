using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Desktop.Persistence
{
    public class IconPackRepository : AsyncRepository<IconPack, string>, IIconPackRepository
    {
        public IconPackRepository(Database database) : base(database)
        {
        }
    }
}
